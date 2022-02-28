// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public sealed class CancelWorkitemDatasetValidator : WorkitemDatasetValidator
    {
        protected override void OnValidate(DicomDataset dataset)
        {
            // Validate the final-state requirements
            ValidateFinalStateRequirements(dataset);
        }

        private static void ValidateFinalStateRequirements(DicomDataset dataset)
        {
            ProcedureStepState targetState = dataset.GetProcedureState();

            dataset.ValidateRequirement(DicomTag.TransactionUID, FinalStateRequirementCode.O, targetState);

            // SOP Common Module

            // Refer: https://dicom.nema.org/medical/dicom/current/output/chtml/part03/sect_C.12.html#sect_C.12.1.1.2
            dataset.ValidateRequirement(DicomTag.SpecificCharacterSet, FinalStateRequirementCode.RC, targetState);

            dataset.ValidateRequirement(DicomTag.SOPClassUID, FinalStateRequirementCode.R, targetState);
            dataset.ValidateRequirement(DicomTag.SOPInstanceUID, FinalStateRequirementCode.R, targetState);

            // Unified Procedure Step Scheduled Procedure Information Module
            dataset.ValidateRequirement(DicomTag.ScheduledProcedureStepPriority, FinalStateRequirementCode.R, targetState);
            dataset.ValidateRequirement(DicomTag.ScheduledProcedureStepModificationDateTime, FinalStateRequirementCode.R, targetState);
            dataset.ValidateRequirement(DicomTag.ScheduledProcedureStepStartDateTime, FinalStateRequirementCode.R, targetState);
            dataset.ValidateRequirement(DicomTag.InputReadinessState, FinalStateRequirementCode.R, targetState);

            // Unified Procedure Step Relationship Module

            // Patient Demographic Module

            // Patient Medical Module

            // Visit Identification Module

            // Visit Status Module

            // Visit Admission Module

            // Unified Procedure Step Progress Information Module
            dataset.ValidateRequirement(DicomTag.ProcedureStepState, FinalStateRequirementCode.R, targetState);
            dataset.ValidateRequirement(DicomTag.ProcedureStepProgressInformationSequence, FinalStateRequirementCode.X, targetState);
            dataset.ValidateRequirement(DicomTag.ProcedureStepCancellationDateTime, FinalStateRequirementCode.X, targetState);
            dataset.ValidateRequirement(DicomTag.ProcedureStepDiscontinuationReasonCodeSequence, FinalStateRequirementCode.X, targetState);

            // Unified Procedure Step Performed Procedure Information Module
            dataset.ValidateRequirement(DicomTag.UnifiedProcedureStepPerformedProcedureSequence, FinalStateRequirementCode.P, targetState);
            dataset.ValidateRequirement(DicomTag.ActualHumanPerformersSequence, FinalStateRequirementCode.RC, targetState);
            dataset.ValidateRequirement(DicomTag.HumanPerformerCodeSequence, FinalStateRequirementCode.RC, targetState);
            dataset.ValidateRequirement(DicomTag.HumanPerformerName, FinalStateRequirementCode.RC, targetState);
            dataset.ValidateRequirement(DicomTag.PerformedStationNameCodeSequence, FinalStateRequirementCode.P, targetState);
            dataset.ValidateRequirement(DicomTag.PerformedProcedureStepStartDateTime, FinalStateRequirementCode.P, targetState);
            dataset.ValidateRequirement(DicomTag.PerformedWorkitemCodeSequence, FinalStateRequirementCode.P, targetState);
            dataset.ValidateRequirement(DicomTag.PerformedProcedureStepEndDateTime, FinalStateRequirementCode.P, targetState);
            dataset.ValidateRequirement(DicomTag.OutputInformationSequence, FinalStateRequirementCode.P, targetState);
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
        }
    }
}
