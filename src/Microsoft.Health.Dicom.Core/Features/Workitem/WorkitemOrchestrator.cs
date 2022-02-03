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
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
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
                    .AddWorkitemAsync(partitionKey, dataset, queryTags, cancellationToken)
                    .ConfigureAwait(false);

                identifier = new WorkitemInstanceIdentifier(
                    dataset.GetSingleValueOrDefault(DicomTag.AffectedSOPInstanceUID, string.Empty),
                    workitemKey,
                    partitionKey);

                // We have successfully created the index, store the file.
                await StoreWorkitemBlobAsync(identifier, dataset, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                await TryDeleteWorkitemAsync(identifier, cancellationToken).ConfigureAwait(false);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<QueryWorkitemResourceResponse> QueryAsync(
            BaseQueryParameters parameters,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(parameters);

            var queryTags = await _workitemQueryTagService.GetQueryTagsAsync(cancellationToken: cancellationToken);

            BaseQueryExpression queryExpression = _queryParser.Parse(parameters, queryTags);

            var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

            WorkitemQueryResult queryResult = await _indexWorkitemStore.QueryAsync(partitionKey, queryExpression, cancellationToken);

            IEnumerable<DicomDataset> workitems = await Task.WhenAll(
                queryResult.WorkitemInstances.Select(x => _workitemStore.GetWorkitemAsync(x, cancellationToken)));

            IEnumerable<DicomDataset> workitemResponses = workitems.Select(m => WorkitemQueryResponseBuilder.GenerateResponseDataset(m, queryExpression));

            return new QueryWorkitemResourceResponse(workitemResponses);
        }

        /// <inheritdoc />
        private async Task TryDeleteWorkitemAsync(
            WorkitemInstanceIdentifier identifier,
            CancellationToken cancellationToken)
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

        private async Task StoreWorkitemBlobAsync(
            WorkitemInstanceIdentifier identifier,
            DicomDataset dicomDataset,
            CancellationToken cancellationToken)
            => await _workitemStore
                .AddWorkitemAsync(identifier, dicomDataset, cancellationToken)
                .ConfigureAwait(false);
    }
}
