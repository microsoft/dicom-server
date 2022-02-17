// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to orchestrate the DICOM workitem instance add, retrieve, cancel, and update.
    /// </summary>
    public class WorkitemOrchestrator : IWorkitemOrchestrator
    {
        private readonly IDicomRequestContextAccessor _contextAccessor;
        private readonly IIndexWorkitemStore _indexWorkitemStore;
        private readonly IWorkitemStore _workitemStore;
        private readonly IWorkitemQueryTagService _workitemQueryTagService;
        private readonly ILogger<WorkitemOrchestrator> _logger;
        private readonly IQueryParser<BaseQueryExpression, BaseQueryParameters> _queryParser;

        public WorkitemOrchestrator(
            IDicomRequestContextAccessor contextAccessor,
            IWorkitemStore workitemStore,
            IIndexWorkitemStore indexWorkitemStore,
            IWorkitemQueryTagService workitemQueryTagService,
            IQueryParser<BaseQueryExpression, BaseQueryParameters> queryParser,
            ILogger<WorkitemOrchestrator> logger)
        {
            _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
            _indexWorkitemStore = EnsureArg.IsNotNull(indexWorkitemStore, nameof(indexWorkitemStore));
            _workitemStore = EnsureArg.IsNotNull(workitemStore, nameof(workitemStore));
            _queryParser = EnsureArg.IsNotNull(queryParser, nameof(queryParser));
            _workitemQueryTagService = EnsureArg.IsNotNull(workitemQueryTagService, nameof(workitemQueryTagService));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        /// <inheritdoc />
        public async Task<WorkitemMetadataStoreEntry> GetWorkitemMetadataAsync(
            string workitemUid,
            CancellationToken cancellationToken = default)
        {
            var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

            var workitemMetadata = await _indexWorkitemStore
                .GetWorkitemMetadataAsync(partitionKey, workitemUid, cancellationToken)
                .ConfigureAwait(false);

            return workitemMetadata;
        }

        /// <inheritdoc />
        public async Task AddWorkitemAsync(DicomDataset dataset, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            WorkitemInstanceIdentifier identifier = null;

            try
            {
                var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();
                var queryTags = await _workitemQueryTagService.GetQueryTagsAsync(cancellationToken).ConfigureAwait(false);

                identifier = await _indexWorkitemStore
                    .BeginAddWorkitemAsync(partitionKey, dataset, queryTags, cancellationToken)
                    .ConfigureAwait(false);

                // We have successfully created the index, store the file.
                await StoreWorkitemBlobAsync(identifier, dataset, null, cancellationToken)
                    .ConfigureAwait(false);

                await _indexWorkitemStore
                    .EndAddWorkitemAsync(identifier.PartitionKey, identifier.WorkitemKey, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                await TryAddWorkitemCleanupAsync(identifier, cancellationToken)
                    .ConfigureAwait(false);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task CancelWorkitemAsync(DicomDataset dataset, WorkitemMetadataStoreEntry workitemMetadata, ProcedureStepState targetProcedureStepState, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(workitemMetadata, nameof(workitemMetadata));

            if (workitemMetadata.Status != WorkitemStoreStatus.ReadWrite)
            {
                return;
            }

            (long CurrentWatermark, long NextWatermark)? watermarkEntry = null;
            WorkitemInstanceIdentifier identifier = null;

            try
            {
                watermarkEntry = await _indexWorkitemStore
                    .GetCurrentAndNextWorkitemWatermarkAsync(workitemMetadata.WorkitemKey, cancellationToken)
                    .ConfigureAwait(false);

                // Get the workitem from blob store
                identifier = new WorkitemInstanceIdentifier(
                    workitemMetadata.WorkitemUid,
                    workitemMetadata.WorkitemKey,
                    workitemMetadata.PartitionKey,
                    watermarkEntry.Value.CurrentWatermark);
                var storeDicomDataset = await GetWorkitemBlobAsync(identifier, cancellationToken).ConfigureAwait(false);

                // update the procedure step state
                var updatedDicomDataset = new DicomDataset(storeDicomDataset);

                // Add/Update the procedure step state in the blob
                var targetProcedureStepStateStringValue = targetProcedureStepState.GetStringValue();
                updatedDicomDataset.AddOrUpdate(DicomTag.ProcedureStepState, targetProcedureStepStateStringValue);

                // if there is a reason for cancellation, set it in the blob
                dataset.TryGetString(DicomTag.ReasonForCancellation, out var cancellationReason);
                if (!string.IsNullOrWhiteSpace(cancellationReason))
                {
                    updatedDicomDataset.AddOrUpdate(DicomTag.ReasonForCancellation, cancellationReason);
                }

                // store the blob with the new watermark
                await StoreWorkitemBlobAsync(identifier, updatedDicomDataset, watermarkEntry.Value.NextWatermark, cancellationToken)
                    .ConfigureAwait(false);

                // Update the workitem procedure step state in the store
                await _indexWorkitemStore
                    .UpdateWorkitemProcedureStepStateAsync(
                        workitemMetadata,
                        watermarkEntry.Value.NextWatermark,
                        targetProcedureStepStateStringValue,
                        cancellationToken)
                    .ConfigureAwait(false);

                // Delete the blob with the old watermark
                await TryDeleteWorkitemBlobAsync(identifier, watermarkEntry.Value.CurrentWatermark, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // attempt to delete the blob with proposed watermark
                if (watermarkEntry.HasValue)
                {
                    await TryDeleteWorkitemBlobAsync(identifier, watermarkEntry.Value.NextWatermark, cancellationToken)
                        .ConfigureAwait(false);
                }

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<QueryWorkitemResourceResponse> QueryAsync(BaseQueryParameters parameters, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(parameters);

            var queryTags = await _workitemQueryTagService.GetQueryTagsAsync(cancellationToken: cancellationToken);

            BaseQueryExpression queryExpression = _queryParser.Parse(parameters, queryTags);

            var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

            WorkitemQueryResult queryResult = await _indexWorkitemStore
                .QueryAsync(partitionKey, queryExpression, cancellationToken);

            IEnumerable<DicomDataset> workitems = await Task.WhenAll(
                queryResult.WorkitemInstances
                    .Select(x => _workitemStore.GetWorkitemAsync(x, cancellationToken)));

            var workitemResponses = workitems.Select(m => WorkitemQueryResponseBuilder.GenerateResponseDataset(m, queryExpression)).ToList();

            return new QueryWorkitemResourceResponse(workitemResponses);
        }

        /// <inheritdoc />
        private async Task TryAddWorkitemCleanupAsync(WorkitemInstanceIdentifier identifier, CancellationToken cancellationToken)
        {
            if (null == identifier)
            {
                return;
            }

            try
            {
                // Cleanup workitem data store
                await _indexWorkitemStore
                    .DeleteWorkitemAsync(identifier, cancellationToken)
                    .ConfigureAwait(false);

                // Cleanup Blob store
                await TryDeleteWorkitemBlobAsync(identifier, null, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, @"Failed to cleanup workitem {Identifier}.", identifier);
            }
        }

        private async Task<DicomDataset> GetWorkitemBlobAsync(
            WorkitemInstanceIdentifier identifier,
            CancellationToken cancellationToken = default)
        {
            if (null == identifier)
            {
                return null;
            }

            return await _workitemStore
                .GetWorkitemAsync(identifier, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task StoreWorkitemBlobAsync(
            WorkitemInstanceIdentifier identifier,
            DicomDataset dicomDataset,
            long? proposedWatermark = default,
            CancellationToken cancellationToken = default)
        {
            if (null == identifier || null == dicomDataset)
            {
                return;
            }

            await _workitemStore
                .AddWorkitemAsync(identifier, dicomDataset, proposedWatermark, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task TryDeleteWorkitemBlobAsync(
            WorkitemInstanceIdentifier identifier,
            long? proposedWatermark = default,
            CancellationToken cancellationToken = default)
        {
            if (null == identifier)
            {
                return;
            }

            try
            {
                await _workitemStore
                    .DeleteWorkitemAsync(identifier, proposedWatermark, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, @"Failed to delete workitem blob for [WorkitemUid: '{WorkitemUid}', PartitionKey: '{PartitionKey}'].", identifier.WorkitemUid, identifier.PartitionKey);
            }
        }
    }
}
