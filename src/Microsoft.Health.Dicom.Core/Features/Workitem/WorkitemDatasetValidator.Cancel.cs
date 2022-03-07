// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public sealed class CancelWorkitemDatasetValidator : WorkitemDatasetValidator
    {
        protected override void OnValidate(DicomDataset dataset)
        {
            // Validate the final-state requirements
            dataset.ValidateFinalStateRequirement();
        }

        /// <summary>
        /// Validates Workitem state in the store and procedure step state transition validity.
        ///
        /// Throws <see cref="WorkitemNotFoundException"/> when workitem-metadata is null.
        /// Throws <see cref="DatasetValidationException"/> when the workitem-metadata status is not read-write.
        /// Throws <see cref="DatasetValidationException"/> when the workitem-metadata transition state has error.
        /// 
        /// </summary>
        /// <param name="workitemUid">The Workitem Uid</param>
        /// <param name="workitemMetadata">The Workitem Metadata</param>
        /// <param name="stateTransitionResult">The state transition result</param>
        public static void ValidateWorkitemState(
            string workitemUid,
            WorkitemMetadataStoreEntry workitemMetadata,
            WorkitemStateTransitionResult stateTransitionResult)
        {
            EnsureArg.IsNotNull(stateTransitionResult, nameof(stateTransitionResult));

            if (workitemMetadata == null)
            {
                throw new WorkitemNotFoundException(workitemUid);
            }

            if (workitemMetadata.Status != WorkitemStoreStatus.ReadWrite)
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.WorkitemUpdateIsNotAllowed,
                        workitemUid,
                        workitemMetadata.ProcedureStepState.GetStringValue()));
            }

            if (stateTransitionResult.IsError)
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.InvalidProcedureStepStateTransition,
                        workitemUid,
                        ProcedureStepState.Canceled,
                        stateTransitionResult.Code));
            }

            if (workitemMetadata.ProcedureStepState == ProcedureStepState.Completed)
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.UpsIsAlreadyCompleted,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.WorkitemIsAlreadyCompleted,
                        workitemUid));
            }

            if (workitemMetadata.ProcedureStepState == ProcedureStepState.Canceled)
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.UpsIsAlreadyCanceled,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.WorkitemIsAlreadyCanceled,
                        workitemUid));
            }
        }
    }
}
