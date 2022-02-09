// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Workitem;
using DicomValidationException = FellowOakDicom.DicomValidationException;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to process the list of <see cref="IDicomInstanceEntry"/>.
    /// </summary>
    public partial class WorkitemService
    {
        private static readonly Action<ILogger, ushort, Exception> LogFailedToAddDelegate =
            LoggerMessage.Define<ushort>(
                LogLevel.Warning,
                default,
                "Failed to add the DICOM instance work-item entry. Failure code: {FailureCode}.");

        private static readonly Action<ILogger, Exception> LogSuccessfullyAddedDelegate =
            LoggerMessage.Define(
                LogLevel.Information,
                default,
                "Successfully added the DICOM instance work-item entry.");

        public async Task<AddWorkitemResponse> ProcessAddAsync(DicomDataset dataset, string workitemInstanceUid, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            // The format of the identifiers will be validated by fo-dicom.
            var workitemUid = string.IsNullOrEmpty(workitemInstanceUid)
                ? (dataset.TryGetString(DicomTag.SOPInstanceUID, out var sopInstanceUid)
                    ? sopInstanceUid
                    : dataset.TryGetString(DicomTag.AffectedSOPInstanceUID, out var affectedSopInstanceUid) ? affectedSopInstanceUid : null)
                : workitemInstanceUid;

            if (ValidateAddRequest(dataset, workitemUid))
            {
                PrepareAddRequest(dataset, workitemUid);

                await AddWorkitemAsync(dataset, cancellationToken).ConfigureAwait(false);
            }

            return _responseBuilder.BuildAddResponse();
        }

        private static void PrepareAddRequest(DicomDataset dataset, string workitemUid)
        {
            var currentProcedureStepState = dataset.GetProcedureState();
            var result = currentProcedureStepState.GetTransitionState(WorkitemStateEvents.NCreate);

            dataset.AddOrUpdate(DicomTag.ProcedureStepState, result.State);
            dataset.AddOrUpdate(DicomTag.SOPInstanceUID, workitemUid);
        }

        private bool ValidateAddRequest(DicomDataset dataset, string workitemInstanceUid)
        {
            try
            {
                GetValidator<AddWorkitemDatasetValidator>().Validate(dataset, workitemInstanceUid);
                return true;
            }
            catch (Exception ex)
            {
                ushort failureCode = FailureReasonCodes.ProcessingFailure;

                switch (ex)
                {
                    case DicomValidationException _:
                        failureCode = FailureReasonCodes.ValidationFailure;
                        break;

                    case DatasetValidationException dicomDatasetValidationException:
                        failureCode = dicomDatasetValidationException.FailureCode;
                        break;

                    case ValidationException _:
                        failureCode = FailureReasonCodes.ValidationFailure;
                        break;
                }

                LogValidationFailedDelegate(_logger, failureCode, ex);

                _responseBuilder.AddFailure(dataset, failureCode, ex.Message);

                return false;
            }
        }

        private async Task AddWorkitemAsync(DicomDataset dataset, CancellationToken cancellationToken)
        {
            try
            {
                await _workitemOrchestrator.AddWorkitemAsync(dataset, cancellationToken).ConfigureAwait(false);

                LogSuccessfullyAddedDelegate(_logger, null);

                _responseBuilder.AddSuccess(dataset);
            }
            catch (Exception ex)
            {
                ushort failureCode = FailureReasonCodes.ProcessingFailure;

                switch (ex)
                {
                    case WorkitemAlreadyExistsException _:
                        failureCode = FailureReasonCodes.SopInstanceAlreadyExists;
                        break;
                }

                LogFailedToAddDelegate(_logger, failureCode, ex);

                // TODO: This can return the Database Error as is. We need to abstract that detail.
                _responseBuilder.AddFailure(dataset, failureCode, ex.Message);
            }
        }
    }
}
