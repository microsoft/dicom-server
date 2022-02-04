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
        private static readonly Action<ILogger, ushort, Exception> LogFailedToCancelDelegate =
            LoggerMessage.Define<ushort>(
                LogLevel.Warning,
                default,
                "Failed to cancel the work-item entry. Failure code: {FailureCode}.");

        private static readonly Action<ILogger, Exception> LogSuccessfullyCanceledDelegate =
            LoggerMessage.Define(
                LogLevel.Information,
                default,
                "Successfully canceled the work-item entry.");

        public async Task<CancelWorkitemResponse> ProcessCancelAsync(DicomDataset dataset, string workitemInstanceUid, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            var workitemMetadata = await _workitemOrchestrator.GetWorkitemMetadataAsync(workitemInstanceUid, cancellationToken).ConfigureAwait(false);

            // Get the state transition result
            var transitionStateResult = ProcedureStepState.GetTransitionState(WorkitemStateEvents.NActionToRequestCancel,
                workitemMetadata?.ProcedureStepState ?? string.Empty);

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
                ushort failureCode = FailureReasonCodes.ProcessingFailure;

                switch (ex)
                {
                    case DicomValidationException _:
                        // TODO: handle this to return 409
                        failureCode = FailureReasonCodes.ValidationFailure;
                        break;

                    case DatasetValidationException dicomDatasetValidationException:
                        // TODO: handle this to return 400
                        failureCode = dicomDatasetValidationException.FailureCode;
                        break;

                    case ValidationException _:
                        // TODO: handle this to return 409
                        failureCode = FailureReasonCodes.ValidationFailure;
                        break;

                    case WorkitemNotFoundException:
                        // TODO: handle this to return 404
                        break;
                }

                LogValidationFailedDelegate(_logger, failureCode, ex);

                _responseBuilder.AddFailure(dataset, failureCode, ex.Message);

                return false;
            }
        }

        private async Task CancelWorkitemAsync(DicomDataset dataset, WorkitemMetadataStoreEntry workitemMetadata, string targetProcedureStepState, CancellationToken cancellationToken)
        {
            try
            {
                await _workitemOrchestrator.CancelWorkitemAsync(dataset, workitemMetadata, targetProcedureStepState, cancellationToken).ConfigureAwait(false);

                LogSuccessfullyCanceledDelegate(_logger, null);

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
                        // TODO: Should this also be treated as FailureReasonCodes.ValidationFailure???
                        failureCode = FailureReasonCodes.ProcessingFailure;
                        break;
                }

                LogFailedToCancelDelegate(_logger, failureCode, ex);

                _responseBuilder.AddFailure(dataset, failureCode, ex.Message);
            }
        }
    }
}
