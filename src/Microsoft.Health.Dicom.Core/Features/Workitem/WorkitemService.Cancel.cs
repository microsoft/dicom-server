// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to process the list of <see cref="IDicomInstanceEntry"/>.
    /// </summary>
    public partial class WorkitemService
    {
        public async Task<CancelWorkitemResponse> ProcessCancelAsync(DicomDataset dataset, string workitemInstanceUid, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            var workitemMetadata = await _workitemOrchestrator
                .GetWorkitemMetadataAsync(workitemInstanceUid, cancellationToken)
                .ConfigureAwait(false);

            // Get the state transition result
            var transitionStateResult = workitemMetadata?
                .ProcedureStepState
                .GetTransitionState(WorkitemStateEvents.NActionToRequestCancel);

            var cancelRequestDataset = await PrepareRequestCancelWorkitemBlobDatasetAsync(
                    dataset, workitemMetadata, transitionStateResult.State, cancellationToken)
                .ConfigureAwait(false);

            if (ValidateCancelRequest(workitemInstanceUid, workitemMetadata, cancelRequestDataset, transitionStateResult))
            {
                // If there is a warning code, the workitem is already in the canceled state.
                if (transitionStateResult.HasWarningWithCode)
                {
                    _responseBuilder.AddSuccess(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            DicomCoreResource.WorkitemIsInFinalState,
                            workitemInstanceUid,
                            workitemMetadata.ProcedureStepState,
                            transitionStateResult.Code));
                }
                else
                {
                    await CancelWorkitemAsync(
                            cancelRequestDataset,
                            workitemMetadata,
                            transitionStateResult.State,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            return _responseBuilder.BuildCancelResponse();
        }

        private async Task<DicomDataset> PrepareRequestCancelWorkitemBlobDatasetAsync(
            DicomDataset dataset,
            WorkitemMetadataStoreEntry workitemMetadata,
            ProcedureStepState targetProcedureStepState,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get the workitem from blob store
                var workitemDataset = await _workitemOrchestrator
                    .GetWorkitemBlobAsync(workitemMetadata, cancellationToken)
                    .ConfigureAwait(false);

                PopulateCancelRequestAttributes(workitemDataset, dataset, targetProcedureStepState);

                return workitemDataset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, @"Error while preparing Cancel Request Blob Dataset");

                throw;
            }
        }

        private static DicomDataset PopulateCancelRequestAttributes(
            DicomDataset workitemDataset,
            DicomDataset cancelRequestDataset,
            ProcedureStepState procedureStepState)
        {
            workitemDataset.AddOrUpdate(DicomTag.ProcedureStepCancellationDateTime, DateTime.UtcNow);
            workitemDataset.AddOrUpdate(DicomTag.ProcedureStepState, procedureStepState.GetStringValue());

            var discontinuationReasonCodeSequence = new DicomDataset();
            if (cancelRequestDataset.TryGetString(DicomTag.ReasonForCancellation, out var cancellationReason))
            {
                discontinuationReasonCodeSequence.Add(DicomTag.ReasonForCancellation, cancellationReason);
            }
            workitemDataset.AddOrUpdate(new DicomSequence(DicomTag.ProcedureStepDiscontinuationReasonCodeSequence, discontinuationReasonCodeSequence));

            if (!workitemDataset.TryGetSequence(DicomTag.ProcedureStepProgressInformationSequence, out var progressInformationSequence))
            {
                progressInformationSequence = new DicomSequence(DicomTag.ProcedureStepProgressInformationSequence);
                workitemDataset.Add(DicomTag.ProcedureStepProgressInformationSequence, progressInformationSequence);
            }
            progressInformationSequence.Items.Add(cancelRequestDataset);

            return workitemDataset;
        }

        private bool ValidateCancelRequest(
            string workitemInstanceUid,
            WorkitemMetadataStoreEntry workitemMetadata,
            DicomDataset dataset,
            WorkitemStateTransitionResult transitionStateResult)
        {
            try
            {
                CancelWorkitemDatasetValidator.ValidateWorkitemState(
                    workitemInstanceUid,
                    workitemMetadata,
                    transitionStateResult);

                GetValidator<CancelWorkitemDatasetValidator>().Validate(dataset);

                return true;
            }
            catch (Exception ex)
            {
                ushort? failureCode = FailureReasonCodes.ProcessingFailure;

                switch (ex)
                {
                    case DatasetValidationException datasetValidationException:
                        // should return 400
                        failureCode = datasetValidationException.FailureCode;
                        break;

                    case DicomValidationException _:
                    case ValidationException _:
                        // should return 409
                        failureCode = FailureReasonCodes.DatasetDoesNotMatchSOPClass;
                        break;

                    case WorkitemNotFoundException:
                        // should return 404
                        failureCode = null;
                        break;
                }

                _logger.LogInformation(ex,
                    "Validation failed for the DICOM instance work-item entry. Failure code: {FailureCode}.", failureCode);

                _responseBuilder.AddFailure(failureCode, ex.Message, dataset);

                return false;
            }
        }

        private async Task CancelWorkitemAsync(
            DicomDataset dataset,
            WorkitemMetadataStoreEntry workitemMetadata,
            ProcedureStepState targetProcedureStepState,
            CancellationToken cancellationToken)
        {
            try
            {
                await _workitemOrchestrator
                    .UpdateWorkitemStateAsync(dataset, workitemMetadata, targetProcedureStepState, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation("Successfully canceled the work-item entry.");

                _responseBuilder.AddSuccess(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.WorkitemCancelRequestSuccess,
                        workitemMetadata.WorkitemUid));
            }
            catch (Exception ex)
            {
                ushort failureCode = FailureReasonCodes.ProcessingFailure;

                switch (ex)
                {
                    case WorkitemNotFoundException _:
                        failureCode = FailureReasonCodes.ProcessingFailure;
                        break;
                }

                _logger.LogWarning(ex, "Failed to cancel the work-item entry. Failure code: {FailureCode}.", failureCode);

                _responseBuilder.AddFailure(failureCode, ex.Message, dataset);
            }
        }
    }
}
