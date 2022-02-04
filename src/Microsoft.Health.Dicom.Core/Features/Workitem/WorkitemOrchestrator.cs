// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;

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

        public WorkitemOrchestrator(
            IDicomRequestContextAccessor contextAccessor,
            IWorkitemStore workitemStore,
            IIndexWorkitemStore indexWorkitemStore,
            IWorkitemQueryTagService workitemQueryTagService,
            ILogger<WorkitemOrchestrator> logger)
        {
            _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
            _indexWorkitemStore = EnsureArg.IsNotNull(indexWorkitemStore, nameof(indexWorkitemStore));
            _workitemStore = EnsureArg.IsNotNull(workitemStore, nameof(workitemStore));
            _workitemQueryTagService = EnsureArg.IsNotNull(workitemQueryTagService, nameof(workitemQueryTagService));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        /// <inheritdoc />
        public async Task<WorkitemMetadataStoreEntry> GetWorkitemMetadataAsync(string workitemInstanceUid, CancellationToken cancellationToken)
        {
            var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

            var workitemMetadata = await _indexWorkitemStore
                .GetWorkitemMetadataAsync(partitionKey, workitemInstanceUid, cancellationToken)
                .ConfigureAwait(false);

            return workitemMetadata;
        }

        /// <inheritdoc />
        public async Task AddWorkitemAsync(
            DicomDataset dataset,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            WorkitemInstanceIdentifier identifier = null;

            try
            {
                int partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

                IReadOnlyCollection<QueryTag> queryTags = await _workitemQueryTagService.GetQueryTagsAsync(cancellationToken).ConfigureAwait(false);

                long workitemKey = await _indexWorkitemStore
                    .BeginAddWorkitemAsync(partitionKey, dataset, queryTags, cancellationToken)
                    .ConfigureAwait(false);

                identifier = new WorkitemInstanceIdentifier(
                    dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                    workitemKey,
                    partitionKey);

                // We have successfully created the index, store the file.
                await StoreWorkitemBlobAsync(identifier, dataset, cancellationToken)
                    .ConfigureAwait(false);

                await _indexWorkitemStore.EndAddWorkitemAsync(partitionKey, workitemKey, cancellationToken);
            }
            catch
            {
                await TryDeleteWorkitemAsync(identifier, cancellationToken).ConfigureAwait(false);

                throw;
            }
        }

        public async Task CancelWorkitemAsync(DicomDataset dataset, WorkitemMetadataStoreEntry workitemMetadata, string targetProcedureStepState, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(workitemMetadata, nameof(workitemMetadata));

            if (workitemMetadata.Status != WorkitemStoreStatus.ReadWrite)
            {
                return;
            }

            WorkitemInstanceIdentifier workitemInstanceIdentifier = null;
            DicomDataset storeDicomDataset = null;

            try
            {
                // soft-lock the workitem
                await _indexWorkitemStore
                    .BeginUpdateWorkitemAsync(workitemMetadata, cancellationToken)
                    .ConfigureAwait(false);

                // Get the workitem from blob store
                workitemInstanceIdentifier = new WorkitemInstanceIdentifier(workitemMetadata.WorkitemUid, workitemMetadata.WorkitemKey, workitemMetadata.PartitionKey);
                storeDicomDataset = await GetWorkitemBlobAsync(workitemInstanceIdentifier, cancellationToken).ConfigureAwait(false);

                // update the procedure step state
                var updatedDicomDataset = new DicomDataset(storeDicomDataset);
                updatedDicomDataset.AddOrUpdate(DicomTag.ProcedureStepState, targetProcedureStepState);

                // if there is a reason for cancellation, update the workitem in the blob store
                dataset.TryGetString(DicomTag.ReasonForCancellation, out var cancellationReason);
                if (!string.IsNullOrWhiteSpace(cancellationReason))
                {
                    updatedDicomDataset.AddOrUpdate(DicomTag.ReasonForCancellation, cancellationReason);

                    await StoreWorkitemBlobAsync(workitemInstanceIdentifier, updatedDicomDataset, cancellationToken).ConfigureAwait(false);
                }

                // Update the workitem tags in the store
                var queryTags = await _workitemQueryTagService.GetQueryTagsAsync(cancellationToken).ConfigureAwait(false);
                await _indexWorkitemStore
                    .EndUpdateWorkitemAsync(workitemMetadata, updatedDicomDataset, queryTags, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Cleanup cancel action
                await TryCleanupCancelAsync(workitemMetadata, workitemInstanceIdentifier, storeDicomDataset, cancellationToken);

                throw;
            }
        }

        private async Task TryCleanupCancelAsync(WorkitemMetadataStoreEntry workitemMetada, WorkitemInstanceIdentifier identifier, DicomDataset dataset, CancellationToken cancellationToken)
        {
            if (null == identifier && null == dataset)
            {
                return;
            }

            try
            {
                // Upload the original blobl back in the store
                await StoreWorkitemBlobAsync(identifier, dataset, cancellationToken).ConfigureAwait(false);

                // release the workitem (soft) lock
                await _indexWorkitemStore
                    .UnlockWorkitemAsync(workitemMetada, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    @"Failed to revert the workitem blob for [WorkitemUid: '{WorkitemUid}'] [PartitionKey: '{PartitionKey}'] [WorkitemKey: '{WorkitemKey}'].",
                    identifier.WorkitemUid,
                    identifier.PartitionKey,
                    identifier.WorkitemKey);
            }
        }

        /// <inheritdoc />
        private async Task TryDeleteWorkitemAsync(WorkitemInstanceIdentifier identifier, CancellationToken cancellationToken)
        {
            if (null == identifier)
            {
                return;
            }

            try
            {
                await _indexWorkitemStore
                    .DeleteWorkitemAsync(identifier.PartitionKey, identifier.WorkitemUid, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    @"Failed to cleanup workitem [WorkitemUid: '{WorkitemUid}'] [PartitionKey: '{PartitionKey}'] [WorkitemKey: '{WorkitemKey}'].",
                    identifier.WorkitemUid,
                    identifier.PartitionKey,
                    identifier.WorkitemKey);
            }
        }

        private async Task<DicomDataset> GetWorkitemBlobAsync(
            WorkitemInstanceIdentifier identifier,
            CancellationToken cancellationToken)
            => await _workitemStore
                .GetWorkitemAsync(identifier, cancellationToken)
                .ConfigureAwait(false);

        private async Task StoreWorkitemBlobAsync(
            WorkitemInstanceIdentifier identifier,
            DicomDataset dicomDataset,
            CancellationToken cancellationToken)
            => await _workitemStore
                .AddWorkitemAsync(identifier, dicomDataset, cancellationToken)
                .ConfigureAwait(false);
    }
}
