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

            if (ValidateCancelRequest(workitemInstanceUid, workitemMetadata, dataset, transitionStateResult))
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
                    await CancelWorkitemAsync(dataset, workitemMetadata, transitionStateResult.State, cancellationToken).ConfigureAwait(false);
                }
            }

            return _responseBuilder.BuildCancelResponse();
        }

        private bool ValidateCancelRequest(string workitemInstanceUid, WorkitemMetadataStoreEntry workitemMetadata, DicomDataset dataset, WorkitemStateTransitionResult transitionStateResult)
        {
            try
            {
                GetValidator<CancelWorkitemDatasetValidator>().Validate(dataset, workitemInstanceUid);

                CancelWorkitemDatasetValidator.ValidateProcedureStepStateInStore(workitemInstanceUid, workitemMetadata, transitionStateResult);

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

        private async Task CancelWorkitemAsync(DicomDataset dataset, WorkitemMetadataStoreEntry workitemMetadata, ProcedureStepState targetProcedureStepState, CancellationToken cancellationToken)
        {
            try
            {
                await _workitemOrchestrator.CancelWorkitemAsync(dataset, workitemMetadata, targetProcedureStepState, cancellationToken).ConfigureAwait(false);

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
