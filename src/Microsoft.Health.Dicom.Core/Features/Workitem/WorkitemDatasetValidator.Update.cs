// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement when Updating.
/// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#sect_5.4.2.1">Dicom 3.4.5.4.2.1</see>
/// </summary>
public class UpdateWorkitemDatasetValidator : WorkitemDatasetValidator
{
    /// <summary>
    /// Validate requirements for update-workitem.
    /// Some values are not allowed which are checked explicitly.
    /// All other values, if present, must not be empty.
    /// Reference: https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-3
    /// </summary>
    /// <param name="dataset">Dataset to be validated.</param>
    protected override void OnValidate(DicomDataset dataset)
    {
        // SOP Common Module
        // TODO: validate character set
        ValidateNotPresent(dataset, DicomTag.SOPClassUID);
        ValidateNotPresent(dataset, DicomTag.SOPInstanceUID);
        dataset.ValidateRequirement(DicomTag.InstanceCoercionDateTime, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.InstanceCreatorUID, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.RelatedGeneralSOPClassUID, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.OriginalSpecializedSOPClassUID, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.CodingSchemeIdentificationSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ContextGroupIdentificationSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.MappingResourceIdentificationSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.TimezoneOffsetFromUTC, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ContributingEquipmentSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.InstanceNumber, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.SOPInstanceStatus, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.SOPAuthorizationDateTime, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.SOPAuthorizationComment, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.AuthorizationEquipmentCertificationNumber, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.MACParametersSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.DigitalSignaturesSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.EncryptedAttributesSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.OriginalAttributesSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.HL7StructuredDocumentReferenceSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ReferencedSOPClassUID, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ReferencedSOPInstanceUID, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.LongitudinalTemporalInformationModified, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.QueryRetrieveView, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ConversionSourceAttributesSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ReferencedFrameNumber, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ReferencedSegmentNumber, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ContentQualification, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PrivateDataElement, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PrivateDataElementCharacteristicsSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.InstanceOriginStatus, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.BarcodeValue, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ReferencedDefinedProtocolSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ReferencedPerformedProtocolSequence, RequirementCode.ThreeThree);

        // Unified Procedure Step Scheduled Procedure Information Module
        dataset.ValidateRequirement(DicomTag.ScheduledProcedureStepPriority, RequirementCode.ThreeOne);
        dataset.ValidateRequirement(DicomTag.ProcedureStepLabel, RequirementCode.ThreeOne);
        dataset.ValidateRequirement(DicomTag.WorklistLabel, RequirementCode.ThreeOne);
        dataset.ValidateRequirement(DicomTag.ScheduledProcessingParametersSequence, RequirementCode.ThreeTwo);
        dataset.ValidateRequirement(DicomTag.ScheduledStationNameCodeSequence, RequirementCode.ThreeTwo);
        dataset.ValidateRequirement(DicomTag.ScheduledStationClassCodeSequence, RequirementCode.ThreeTwo);
        dataset.ValidateRequirement(DicomTag.ScheduledStationGeographicLocationCodeSequence, RequirementCode.ThreeTwo);
        dataset.ValidateRequirement(DicomTag.ScheduledHumanPerformersSequence, RequirementCode.ThreeTwo);
        dataset.ValidateRequirement(DicomTag.ScheduledProcedureStepStartDateTime, RequirementCode.ThreeOne);
        dataset.ValidateRequirement(DicomTag.ExpectedCompletionDateTime, RequirementCode.ThreeOne);
        dataset.ValidateRequirement(DicomTag.ScheduledProcedureStepExpirationDateTime, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ScheduledWorkitemCodeSequence, RequirementCode.ThreeOne);
        dataset.ValidateRequirement(DicomTag.CommentsOnTheScheduledProcedureStep, RequirementCode.ThreeOne);
        dataset.ValidateRequirement(DicomTag.InputReadinessState, RequirementCode.ThreeOne);
        dataset.ValidateRequirement(DicomTag.InputInformationSequence, RequirementCode.ThreeTwo);
        dataset.ValidateRequirement(DicomTag.StudyInstanceUID, RequirementCode.ThreeTwo);
        dataset.ValidateRequirement(DicomTag.OutputDestinationSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ScheduledProcedureStepModificationDateTime, RequirementCode.ThreeThree);

        // Unified Procedure Step Relationship Module
        ValidateNotPresent(dataset, DicomTag.PatientName);
        ValidateNotPresent(dataset, DicomTag.PatientID);

        // Issuer of Patient ID Macro
        ValidateNotPresent(dataset, DicomTag.IssuerOfPatientID);
        ValidateNotPresent(dataset, DicomTag.IssuerOfPatientIDQualifiersSequence);

        dataset.ValidateRequirement(DicomTag.OtherPatientIDsSequence, RequirementCode.ThreeThree);
        ValidateNotPresent(dataset, DicomTag.PatientBirthDate);
        ValidateNotPresent(dataset, DicomTag.PatientSex);
        dataset.ValidateRequirement(DicomTag.ReferencedPatientPhotoSequence, RequirementCode.ThreeThree);
        ValidateNotPresent(dataset, DicomTag.AdmissionID);
        ValidateNotPresent(dataset, DicomTag.IssuerOfAdmissionIDSequence);
        ValidateNotPresent(dataset, DicomTag.AdmittingDiagnosesDescription);
        ValidateNotPresent(dataset, DicomTag.AdmittingDiagnosesCodeSequence);
        ValidateNotPresent(dataset, DicomTag.ReferencedRequestSequence);
        ValidateNotPresent(dataset, DicomTag.ReplacedProcedureStepSequence);

        // Patient Demographic Module
        dataset.ValidateRequirement(DicomTag.PatientAge, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.Occupation, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ConfidentialityConstraintOnPatientDataDescription, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientBirthTime, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.QualityControlSubject, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientInsurancePlanCodeSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientPrimaryLanguageCodeSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientSize, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientWeight, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientSizeCodeSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientAddress, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.MilitaryRank, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.BranchOfService, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.CountryOfResidence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.RegionOfResidence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientTelephoneNumbers, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientTelecomInformation, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.EthnicGroup, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientReligiousPreference, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientComments, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ResponsiblePerson, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ResponsiblePersonRole, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ResponsibleOrganization, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientSpeciesDescription, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientSpeciesCodeSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientBreedDescription, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientBreedCodeSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.BreedRegistrationSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.StrainDescription, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.StrainNomenclature, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.StrainCodeSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.StrainAdditionalInformation, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.StrainStockSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.GeneticModificationsSequence, RequirementCode.ThreeThree);

        // Patient Medical Module
        dataset.ValidateRequirement(DicomTag.MedicalAlerts, RequirementCode.ThreeTwo);
        dataset.ValidateRequirement(DicomTag.PregnancyStatus, RequirementCode.ThreeTwo);
        dataset.ValidateRequirement(DicomTag.SpecialNeeds, RequirementCode.ThreeTwo);
        dataset.ValidateRequirement(DicomTag.Allergies, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.SmokingStatus, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.AdditionalPatientHistory, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.LastMenstrualDate, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientSexNeutered, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientBodyMassIndex, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.MeasuredAPDimension, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.MeasuredLateralDimension, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientState, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PertinentDocumentsSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PertinentResourcesSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientClinicalTrialParticipationSequence, RequirementCode.ThreeThree);

        // Visit Identification Module
        dataset.ValidateRequirement(DicomTag.InstitutionName, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.InstitutionAddress, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.InstitutionCodeSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.InstitutionalDepartmentName, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.InstitutionalDepartmentTypeCodeSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ReasonForVisit, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ReasonForVisitCodeSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ServiceEpisodeID, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.IssuerOfServiceEpisodeIDSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ServiceEpisodeDescription, RequirementCode.ThreeThree);

        // Visit Status Module
        dataset.ValidateRequirement(DicomTag.VisitStatusID, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.CurrentPatientLocation, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.PatientInstitutionResidence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.VisitComments, RequirementCode.ThreeThree);

        // Visit Admission Module
        dataset.ValidateRequirement(DicomTag.ReferringPhysicianName, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ReferringPhysicianAddress, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ReferringPhysicianTelephoneNumbers, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ReferringPhysicianIdentificationSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ConsultingPhysicianName, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.ConsultingPhysicianIdentificationSequence, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.RouteOfAdmissions, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.AdmittingDate, RequirementCode.ThreeThree);
        dataset.ValidateRequirement(DicomTag.AdmittingTime, RequirementCode.ThreeThree);

        // Unified Procedure Step Progress Information Module
        ValidateNotPresent(dataset, DicomTag.ProcedureStepState);
        dataset.ValidateRequirement(DicomTag.ProcedureStepProgressInformationSequence, RequirementCode.ThreeTwo);

        // Unified Procedure Step Performed Procedure Information Module
        dataset.ValidateRequirement(DicomTag.UnifiedProcedureStepPerformedProcedureSequence, RequirementCode.ThreeTwo);
    }

    /// <summary>
    /// Validates Workitem state in the store and procedure step state transition validity.
    /// Also validate that the passed Transaction Uid matches the existing transaction Uid.
    /// 
    /// Throws <see cref="WorkitemNotFoundException"/> when workitem-metadata is null.
    /// Throws <see cref="DatasetValidationException"/> when the workitem-metadata status is not read-write.
    /// Throws <see cref="DatasetValidationException"/> when the workitem-metadata procedure step state is not In Progress.
    /// Throws <see cref="DatasetValidationException"/> when the transaction uid does not match the existing transaction uid.
    /// 
    /// </summary>
    /// <param name="transactionUid">The Transaction Uid.</param>
    /// <param name="workitemMetadata">The Workitem Metadata.</param>
    public static void ValidateWorkitemStateAndTransactionUid(
        string transactionUid,
        WorkitemMetadataStoreEntry workitemMetadata)
    {
        if (workitemMetadata == null)
        {
            throw new WorkitemNotFoundException();
        }

        if (workitemMetadata.Status != WorkitemStoreStatus.ReadWrite)
        {
            throw new DatasetValidationException(
                FailureReasonCodes.ProcessingFailure,
                DicomCoreResource.WorkitemCurrentlyBeingUpdated);
        }

        switch (workitemMetadata.ProcedureStepState)
        {
            case ProcedureStepState.Scheduled:
                //  Update can be made when in Scheduled state. Transaction UID cannot be present though.
                if (!string.IsNullOrWhiteSpace(transactionUid))
                {
                    throw new DatasetValidationException(
                        FailureReasonCodes.UpsTransactionUidIncorrect,
                        DicomCoreResource.InvalidTransactionUID);
                }
                break;
            case ProcedureStepState.InProgress:
                // Transaction UID must be provided
                if (string.IsNullOrWhiteSpace(transactionUid))
                {
                    throw new DatasetValidationException(
                        FailureReasonCodes.UpsTransactionUidAbsent,
                        DicomCoreResource.TransactionUIDAbsent);
                }

                // Provided Transaction UID has to be equal to the existing Transaction UID.
                if (!string.Equals(workitemMetadata.TransactionUid, transactionUid, System.StringComparison.Ordinal))
                {
                    throw new DatasetValidationException(
                        FailureReasonCodes.UpsTransactionUidIncorrect,
                        DicomCoreResource.InvalidTransactionUID);
                }

                break;
            case ProcedureStepState.Completed:
                throw new DatasetValidationException(
                    FailureReasonCodes.UpsIsAlreadyCompleted,
                    DicomCoreResource.WorkitemIsAlreadyCompleted);
            case ProcedureStepState.Canceled:
                throw new DatasetValidationException(
                    FailureReasonCodes.UpsIsAlreadyCanceled,
                    DicomCoreResource.WorkitemIsAlreadyCanceled);
        }
    }
}
