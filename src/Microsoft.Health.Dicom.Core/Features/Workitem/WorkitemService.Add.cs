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
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to process the list of <see cref="IDicomInstanceEntry"/>.
    /// </summary>
    public partial class WorkitemService
    {
        private const string WorklistLabel = "worklist";

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

            SetSpecifiedAttributesForCreate(dataset, workitemInstanceUid);

            if (Validate(dataset))
            {
                await AddWorkitemAsync(dataset, cancellationToken).ConfigureAwait(false);
            }

            return _responseBuilder.BuildAddResponse();
        }

        /// <summary>
        /// Sets attributes that are the Service Class Provider's responsibility according to:
        /// <see href='https://dicom.nema.org/dicom/2013/output/chtml/part04/sect_CC.2.html#table_CC.2.5-3'/>
        /// </summary>
        internal static void SetSpecifiedAttributesForCreate(DicomDataset dataset, string workitemQueryParameter)
        {
            // SOP Common Module
            dataset.AddOrUpdate(DicomTag.SOPClassUID, DicomUID.UnifiedProcedureStepPush);
            ReconcileWorkitemInstanceUid(dataset, workitemQueryParameter);

            // Unified Procedure Step Scheduled Procedure Information Module
            dataset.AddOrUpdate(DicomTag.ScheduledProcedureStepModificationDateTime, DateTime.UtcNow);
            dataset.AddOrUpdate(DicomTag.WorklistLabel, WorklistLabel);

            // Unified Procedure Step Progress Information Module
            dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepState.Scheduled);
        }

        /// <summary>
        /// Sets the dataset value from the query parameter as long as there is no conflict.
        /// </summary>
        internal static void ReconcileWorkitemInstanceUid(DicomDataset dataset, string workitemQueryParameter)
        {
            if (!string.IsNullOrWhiteSpace(workitemQueryParameter))
            {
                var uidInDataset = dataset.TryGetString(DicomTag.SOPInstanceUID, out var sopInstanceUid);

                if (uidInDataset && !string.Equals(workitemQueryParameter, sopInstanceUid, StringComparison.Ordinal))
                {
                    throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.MismatchSopInstanceWorkitemInstanceUid,
                        sopInstanceUid,
                        workitemQueryParameter));
                }

                dataset.AddOrUpdate(DicomTag.SOPInstanceUID, workitemQueryParameter);
            }
        }

        private bool Validate(DicomDataset dataset)
        {
            try
            {
                GetValidator<AddWorkitemDatasetValidator>().Validate(dataset);
                return true;
            }
            catch (Exception ex)
            {
                ushort failureCode = FailureReasonCodes.ProcessingFailure;

                switch (ex)
                {
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
