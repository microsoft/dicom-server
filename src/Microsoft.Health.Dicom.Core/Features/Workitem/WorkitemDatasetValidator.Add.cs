// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement when Adding.
    /// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#sect_5.4.2.1">Dicom 3.4.5.4.2.1</see>
    /// </summary>
    public class AddWorkitemDatasetValidator : WorkitemDatasetValidator
    {
        // 2/1 requirements are treated as 1/1 since the value should be set by this point.
        // 3/* and -/- requirements are ignored.
        protected override void OnValidate(DicomDataset dataset)
        {
            // TODO: return all validation exceptions together

            dataset.ValidateRequirement(DicomTag.TransactionUID, RequirementCode.TwoTwo);
            ValidateEmptyValue(dataset, DicomTag.TransactionUID);

            // SOP Common Module
            // TODO: validate character set
            dataset.ValidateRequirement(DicomTag.SOPClassUID, RequirementCode.OneOne);
            dataset.ValidateRequirement(DicomTag.SOPInstanceUID, RequirementCode.OneOne);

            // Unified Procedure Step Scheduled Procedure Information Module
            dataset.ValidateRequirement(DicomTag.ScheduledProcedureStepPriority, RequirementCode.OneOne);
            dataset.ValidateRequirement(DicomTag.ScheduledProcedureStepModificationDateTime, RequirementCode.OneOne);
            dataset.ValidateRequirement(DicomTag.ProcedureStepLabel, RequirementCode.OneOne);
            dataset.ValidateRequirement(DicomTag.WorklistLabel, RequirementCode.OneOne);
            dataset.ValidateRequirement(DicomTag.ScheduledProcessingParametersSequence, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.ScheduledStationNameCodeSequence, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.ScheduledStationNameCodeSequence, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.ScheduledStationClassCodeSequence, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.ScheduledStationGeographicLocationCodeSequence, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.ScheduledStationNameCodeSequence, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.ScheduledProcedureStepStartDateTime, RequirementCode.OneOne);
            dataset.ValidateRequirement(DicomTag.ScheduledWorkitemCodeSequence, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.CommentsOnTheScheduledProcedureStep, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.InputReadinessState, RequirementCode.OneOne);
            dataset.ValidateRequirement(DicomTag.InputInformationSequence, RequirementCode.TwoTwo);

            // Unified Procedure Step Relationship Module
            dataset.ValidateRequirement(DicomTag.PatientName, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.OtherPatientIDsSequence, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.PatientBirthDate, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.PatientSex, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.AdmissionID, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.IssuerOfAdmissionIDSequence, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.AdmittingDiagnosesDescription, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.AdmittingDiagnosesCodeSequence, RequirementCode.TwoTwo);
            dataset.ValidateRequirement(DicomTag.ReferencedRequestSequence, RequirementCode.TwoTwo);

            // Unified Procedure Step Progress Information Module
            dataset.ValidateRequirement(DicomTag.ProcedureStepState, RequirementCode.OneOne);

            dataset.ValidateRequirement(DicomTag.ProcedureStepProgressInformationSequence, RequirementCode.TwoTwo);
            ValidateEmptyValue(dataset, DicomTag.ProcedureStepProgressInformationSequence);

            ValidateNotPresent(dataset, DicomTag.ProcedureStepCancellationDateTime);

            dataset.ValidateRequirement(DicomTag.UnifiedProcedureStepPerformedProcedureSequence, RequirementCode.TwoTwo);
            ValidateEmptyValue(dataset, DicomTag.UnifiedProcedureStepPerformedProcedureSequence);
        }

        private static void ValidateEmptyValue(DicomDataset dataset, DicomTag tag)
        {
            if (dataset.GetValueCount(tag) > 0)
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.AttributeMustBeEmpty,
                        tag));
            }
        }

        private static void ValidateNotPresent(DicomDataset dataset, DicomTag tag)
        {
            if (dataset.Contains(tag))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.AttributeNotAllowed,
                        tag));
            }
        }
    }
}
