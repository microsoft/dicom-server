// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Store;

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
                    dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
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

        public async Task CancelWorkitemAsync(string workitemInstanceUid, DicomDataset dataset, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(workitemInstanceUid, nameof(workitemInstanceUid));

            try
            {
                var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

                // TODO: Need to find a way to move this out of Orchestrator. Should be part of the main validator???
                var futureProcedureStepState = await ValidateProcedureStepStateInStoreAsync(workitemInstanceUid, partitionKey, cancellationToken);

                dataset.AddOrUpdate(DicomTag.ProcedureStepState, futureProcedureStepState);

                var queryTags = await _workitemQueryTagService.GetQueryTagsAsync(cancellationToken).ConfigureAwait(false);
                var workitemKey = await _indexWorkitemStore
                    .UpdateWorkitemAsync(partitionKey, workitemInstanceUid, dataset, queryTags, cancellationToken)
                    .ConfigureAwait(false);

                var workitemInstanceIdentifier = new WorkitemInstanceIdentifier(workitemInstanceUid, workitemKey, partitionKey);

                dataset.TryGetString(DicomTag.ReasonForCancellation, out var cancellationReason);
                if (!string.IsNullOrWhiteSpace(cancellationReason))
                {
                    var blobDicomDataset = await GetWorkitemBlobAsync(workitemInstanceIdentifier, cancellationToken).ConfigureAwait(false);

                    dataset.CopyTo(blobDicomDataset);

                    await StoreWorkitemBlobAsync(workitemInstanceIdentifier, blobDicomDataset, cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
                throw;
            }
        }

        private async Task<string> ValidateProcedureStepStateInStoreAsync(string workitemInstanceUid, int partitionKey, CancellationToken cancellationToken)
        {
            var workitemDetail = await _indexWorkitemStore
                .GetWorkitemDetailAsync(partitionKey, workitemInstanceUid, cancellationToken)
                .ConfigureAwait(false);

            if (workitemDetail == null)
            {
                throw new WorkitemNotFoundException(workitemInstanceUid);
            }

            var transitionStateResult = ProcedureStepState.GetTransitionState(WorkitemStateEvents.NActionToRequestCancel, workitemDetail.ProcedureStepState);
            if (transitionStateResult.IsError)
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.InvalidProcedureStepState,
                        workitemDetail.ProcedureStepState,
                        workitemInstanceUid,
                        transitionStateResult.Code));
            }


            // If the Future state is Empty, then assume that workitem is already in the final state.
            if (string.IsNullOrWhiteSpace(transitionStateResult.State))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.WorkitemIsInFinalState,
                        workitemInstanceUid,
                        workitemDetail.ProcedureStepState,
                        transitionStateResult.Code));
            }

            return transitionStateResult.State;
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
