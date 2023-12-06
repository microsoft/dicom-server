// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// Workitem dataset validator extension
/// </summary>
internal static class WorkitemDatasetValidatorExtension
{
    private static readonly HashSet<RequirementDetail> AddWorkitemRequirements = GetRequirements(WorkitemRequestType.Add);
    private static readonly HashSet<RequirementDetail> UpdateWorkitemRequirements = GetRequirements(WorkitemRequestType.Update);

    public static void ValidateAllRequirements(this DicomDataset dataset, WorkitemRequestType requestType)
    {
        HashSet<RequirementDetail> requirements = null;

        switch (requestType)
        {
            case WorkitemRequestType.Add:
                requirements = AddWorkitemRequirements;
                break;
            case WorkitemRequestType.Update:
                requirements = UpdateWorkitemRequirements;
                break;
        }

        dataset.ValidateAllRequirements(requirements);
    }

    /// <summary>
    /// Refer <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#sect_5.4.2.1" />
    /// </summary>
    /// <param name="requestType">Request type: Add or Update.</param>
    /// <returns>Set containing all the requirements for specified request type.</returns>
    private static HashSet<RequirementDetail> GetRequirements(WorkitemRequestType requestType)
    {
        HashSet<RequirementDetail> requirements = null;

        switch (requestType)
        {
            case WorkitemRequestType.Add:
                GetAddWorkitemRequirements(out requirements);
                break;
            case WorkitemRequestType.Update:
                GetUpdateWorkitemRequirements(out requirements);
                break;
        }

        return requirements;
    }

    /// <summary>
    /// Get validation requirements for Add Workitem dataset.
    /// Reference: https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-3"
    /// </summary>
    /// <param name="requirements">Hashset containing requirements to be validated.</param>
    private static void GetAddWorkitemRequirements(out HashSet<RequirementDetail> requirements)
    {
        requirements = new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.TransactionUID, RequirementCode.TwoTwo),
            new RequirementDetail(DicomTag.TransactionUID, RequirementCode.MustBeEmpty),
        };

        requirements.UnionWith(GetSOPCommonModuleRequirements(WorkitemRequestType.Add));
        requirements.UnionWith(GetUnifiedProcedureStepScheduledProcedureInformationModuleRequirements(WorkitemRequestType.Add));
        requirements.UnionWith(GetUnifiedProcedureStepRelationshipModuleRequirements(WorkitemRequestType.Add));
        requirements.UnionWith(GetPatientDemographicModuleRequirements());
        requirements.UnionWith(GetPatientMedicalModuleRequirements());
        requirements.UnionWith(GetVisitIdentificationModuleRequirements());
        requirements.UnionWith(GetVisitStatusModuleRequirements());
        requirements.UnionWith(GetVisitAdmissionModuleRequirements());
        requirements.UnionWith(GetUnifiedProcedureStepProgressInformationModuleRequirements(WorkitemRequestType.Add));
        requirements.UnionWith(GetUnifiedProcedureStepPerformedProcedureInformationModuleRequirements(WorkitemRequestType.Add));
    }

    /// <summary>
    /// Get validation requirements for Update Workitem dataset.
    /// Reference: https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-3"
    /// </summary>
    /// <param name="requirements">Hashset containing requirements to be validated.</param>
    private static void GetUpdateWorkitemRequirements(out HashSet<RequirementDetail> requirements)
    {
        requirements = new HashSet<RequirementDetail>(GetSOPCommonModuleRequirements(WorkitemRequestType.Update));
        requirements.UnionWith(GetUnifiedProcedureStepScheduledProcedureInformationModuleRequirements(WorkitemRequestType.Update));
        requirements.UnionWith(GetUnifiedProcedureStepRelationshipModuleRequirements(WorkitemRequestType.Update));
        requirements.UnionWith(GetPatientDemographicModuleRequirements());
        requirements.UnionWith(GetPatientMedicalModuleRequirements());
        requirements.UnionWith(GetVisitIdentificationModuleRequirements());
        requirements.UnionWith(GetVisitStatusModuleRequirements());
        requirements.UnionWith(GetVisitAdmissionModuleRequirements());
        requirements.UnionWith(GetUnifiedProcedureStepProgressInformationModuleRequirements(WorkitemRequestType.Update));
        requirements.UnionWith(GetUnifiedProcedureStepPerformedProcedureInformationModuleRequirements(WorkitemRequestType.Update));
    }

    private static HashSet<RequirementDetail> GetSOPCommonModuleRequirements(WorkitemRequestType requestType)
    {
        HashSet<RequirementDetail> requirements = new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.SpecificCharacterSet, RequirementCode.OneCOneC),
            new RequirementDetail(DicomTag.SOPClassUID, requestType == WorkitemRequestType.Add ? RequirementCode.OneOne : RequirementCode.NotAllowed),
            new RequirementDetail(DicomTag.SOPInstanceUID, requestType == WorkitemRequestType.Add ? RequirementCode.OneOne : RequirementCode.NotAllowed),
            new RequirementDetail(DicomTag.InstanceCreationDate, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.InstanceCreationTime, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.InstanceCoercionDateTime, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.InstanceCreatorUID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.RelatedGeneralSOPClassUID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.OriginalSpecializedSOPClassUID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.CodingSchemeIdentificationSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.CodingSchemeDesignator, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.CodingSchemeRegistry, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.CodingSchemeUID, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.CodingSchemeExternalID, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.CodingSchemeName, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.CodingSchemeVersion, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.CodingSchemeResponsibleOrganization, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.CodingSchemeResourcesSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
                {
                    new RequirementDetail(DicomTag.CodingSchemeURLType, RequirementCode.ThreeThree),
                    new RequirementDetail(DicomTag.CodingSchemeURL, RequirementCode.ThreeThree),
                }),
            }),
            new RequirementDetail(DicomTag.ContextGroupIdentificationSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.ContextIdentifier, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.ContextUID, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.MappingResource, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.ContextGroupVersion, RequirementCode.ThreeThree),
            }),
            new RequirementDetail(DicomTag.MappingResourceIdentificationSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.MappingResource, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.MappingResourceUID, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.MappingResourceName, RequirementCode.ThreeThree),
            }),
            new RequirementDetail(DicomTag.TimezoneOffsetFromUTC, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ContributingEquipmentSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.PurposeOfReferenceCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
                new RequirementDetail(DicomTag.Manufacturer, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.InstitutionName, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.InstitutionAddress, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.StationName, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.InstitutionalDepartmentName, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.InstitutionalDepartmentTypeCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
                new RequirementDetail(DicomTag.OperatorsName, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.OperatorIdentificationSequence, RequirementCode.ThreeThree, GetPersonIdentificationMacroAttributesRequirements()),
                new RequirementDetail(DicomTag.ManufacturerModelName, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.DeviceSerialNumber, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.SoftwareVersions, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.DeviceUID, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.UDISequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
                {
                    new RequirementDetail(DicomTag.UniqueDeviceIdentifier, RequirementCode.ThreeThree),
                    new RequirementDetail(DicomTag.DeviceDescription, RequirementCode.ThreeThree),
                }),
                new RequirementDetail(DicomTag.SpatialResolution, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.DateOfLastCalibration, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.TimeOfLastCalibration, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.ContributionDateTime, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.ContributionDescription, RequirementCode.ThreeThree),
            }),
            new RequirementDetail(DicomTag.InstanceNumber, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.SOPInstanceStatus, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.SOPAuthorizationDateTime, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.SOPAuthorizationComment, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.AuthorizationEquipmentCertificationNumber, RequirementCode.ThreeThree),
        };

        requirements.UnionWith(GetDigitalSignatureMacroAttributesRequirements());
        requirements.Add(new RequirementDetail(DicomTag.EncryptedAttributesSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.EncryptedContentTransferSyntaxUID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.EncryptedContent, RequirementCode.ThreeThree),
        }));
        requirements.Add(GetOriginalAttributesMacroAttributesRequirements());
        requirements.Add(GetHL7StructuredDocumentReferenceSequenceRequirements());
        requirements.Add(new RequirementDetail(DicomTag.LongitudinalTemporalInformationModified, RequirementCode.ThreeThree));
        requirements.Add(new RequirementDetail(DicomTag.QueryRetrieveView, RequirementCode.ThreeThree));
        requirements.Add(GetConversionSourceAttributesSequenceRequirements());
        requirements.Add(new RequirementDetail(DicomTag.ContentQualification, RequirementCode.ThreeThree));
        requirements.Add(new RequirementDetail(DicomTag.PrivateDataElementCharacteristicsSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.PrivateGroupReference, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PrivateCreatorReference, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PrivateDataElementDefinitionSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.PrivateDataElement, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.PrivateDataElementValueMultiplicity, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.PrivateDataElementValueRepresentation, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.PrivateDataElementNumberOfItems, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.PrivateDataElementKeyword, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.PrivateDataElementName, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.PrivateDataElementDescription, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.PrivateDataElementEncoding, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.RetrieveURI, RequirementCode.ThreeThree),
            }),
            new RequirementDetail(DicomTag.BlockIdentifyingInformationStatus, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.NonidentifyingPrivateElements, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.DeidentificationActionSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.IdentifyingPrivateElements, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.DeidentificationAction, RequirementCode.ThreeThree),
            }),
        }));
        requirements.Add(new RequirementDetail(DicomTag.InstanceOriginStatus, RequirementCode.ThreeThree));
        requirements.Add(new RequirementDetail(DicomTag.BarcodeValue, RequirementCode.ThreeThree));
        requirements.UnionWith(GetGeneralProcedureProtocolReferenceMacroAttributesRequirements());

        return requirements;
    }

    private static HashSet<RequirementDetail> GetUnifiedProcedureStepScheduledProcedureInformationModuleRequirements(WorkitemRequestType requestType)
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.ScheduledProcedureStepPriority, requestType == WorkitemRequestType.Add ? RequirementCode.OneOne : RequirementCode.ThreeOne),
            new RequirementDetail(DicomTag.ScheduledProcedureStepModificationDateTime, RequirementCode.OneOne),
            new RequirementDetail(DicomTag.ProcedureStepLabel, requestType == WorkitemRequestType.Add ? RequirementCode.OneOne : RequirementCode.ThreeOne),
            new RequirementDetail(DicomTag.WorklistLabel, requestType == WorkitemRequestType.Add ? RequirementCode.TwoOne : RequirementCode.ThreeOne),
            new RequirementDetail(DicomTag.ScheduledProcessingParametersSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.ThreeTwo, GetUPSContentItemMacroRequirements()),
            new RequirementDetail(DicomTag.ScheduledStationNameCodeSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.ThreeTwo, GetUPSCodeSequenceMacroRequirements()),
            new RequirementDetail(DicomTag.ScheduledStationClassCodeSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.ThreeTwo, GetUPSCodeSequenceMacroRequirements()),
            new RequirementDetail(DicomTag.ScheduledStationGeographicLocationCodeSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.ThreeTwo, GetUPSCodeSequenceMacroRequirements()),
            new RequirementDetail(DicomTag.ScheduledHumanPerformersSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoCTwoC : RequirementCode.ThreeTwo, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.HumanPerformerCodeSequence, RequirementCode.OneOne, GetUPSCodeSequenceMacroRequirements()),
                new RequirementDetail(DicomTag.HumanPerformerName, RequirementCode.OneOne),
                new RequirementDetail(DicomTag.HumanPerformerOrganization, RequirementCode.OneOne),
            }),
            new RequirementDetail(DicomTag.ScheduledProcedureStepStartDateTime, requestType == WorkitemRequestType.Add ? RequirementCode.OneOne : RequirementCode.ThreeOne),
            new RequirementDetail(DicomTag.ExpectedCompletionDateTime, RequirementCode.ThreeOne),
            new RequirementDetail(DicomTag.ScheduledProcedureStepExpirationDateTime, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ScheduledWorkitemCodeSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.ThreeOne, GetUPSCodeSequenceMacroRequirements()),
            new RequirementDetail(DicomTag.CommentsOnTheScheduledProcedureStep, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.ThreeOne),
            new RequirementDetail(DicomTag.InputReadinessState, requestType == WorkitemRequestType.Add ? RequirementCode.OneOne : RequirementCode.ThreeOne),
            new RequirementDetail(DicomTag.InputInformationSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.ThreeTwo, GetReferencedInstancesAndAccessMacroRequirements()),
            new RequirementDetail(DicomTag.StudyInstanceUID, requestType == WorkitemRequestType.Add ? RequirementCode.OneCTwo : RequirementCode.ThreeTwo),
            new RequirementDetail(DicomTag.OutputDestinationSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.ReferencedSOPClassUID, RequirementCode.OneCOne),
                new RequirementDetail(DicomTag.DICOMStorageSequence, RequirementCode.OneCOne, new HashSet<RequirementDetail>
                {
                    new RequirementDetail(DicomTag.DestinationAE, RequirementCode.OneOne),
                }),
                new RequirementDetail(DicomTag.STOWRSStorageSequence, RequirementCode.OneCOne, new HashSet<RequirementDetail>
                {
                    new RequirementDetail(DicomTag.StorageURL, RequirementCode.OneOne),
                }),
                new RequirementDetail(DicomTag.XDSStorageSequence, RequirementCode.OneCOne, new HashSet<RequirementDetail>
                {
                    new RequirementDetail(DicomTag.RepositoryUniqueID, RequirementCode.OneOne),
                    new RequirementDetail(DicomTag.HomeCommunityID, RequirementCode.ThreeTwo),
                }),
            }),
        };
    }

    private static HashSet<RequirementDetail> GetUnifiedProcedureStepRelationshipModuleRequirements(WorkitemRequestType requestType)
    {
        HashSet<RequirementDetail> requirements = new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.PatientName, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.NotAllowed),
            new RequirementDetail(DicomTag.PatientID, requestType == WorkitemRequestType.Add ? RequirementCode.OneCTwo : RequirementCode.NotAllowed),
        };

        requirements.UnionWith(GetIssuerOfPatientIDMacroRequirements(requestType));

        requirements.Add(new RequirementDetail(DicomTag.OtherPatientIDsSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.ThreeThree, GetOtherPatientIDSequenceRequirements(requestType)));
        requirements.Add(new RequirementDetail(DicomTag.PatientBirthDate, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.NotAllowed));
        requirements.Add(new RequirementDetail(DicomTag.PatientSex, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.NotAllowed));
        requirements.Add(new RequirementDetail(DicomTag.ReferencedPatientPhotoSequence, RequirementCode.ThreeThree, GetReferencedInstancesAndAccessMacroRequirements()));
        requirements.Add(new RequirementDetail(DicomTag.AdmissionID, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.NotAllowed));
        requirements.Add(new RequirementDetail(DicomTag.IssuerOfAdmissionIDSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.NotAllowed, GetHL7v2HierarchicDesignatorMacroForAddRequirements()));
        requirements.Add(new RequirementDetail(DicomTag.AdmittingDiagnosesDescription, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.NotAllowed));
        requirements.Add(new RequirementDetail(DicomTag.AdmittingDiagnosesCodeSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.NotAllowed, GetUPSCodeSequenceMacroRequirements()));
        requirements.Add(new RequirementDetail(DicomTag.ReferencedRequestSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.NotAllowed, new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.StudyInstanceUID, RequirementCode.OneOne),
            new RequirementDetail(DicomTag.AccessionNumber, RequirementCode.TwoTwo),
            new RequirementDetail(DicomTag.IssuerOfAccessionNumberSequence, RequirementCode.TwoTwo, GetHL7v2HierarchicDesignatorMacroForAddRequirements()),
            new RequirementDetail(DicomTag.PlacerOrderNumberImagingServiceRequest, RequirementCode.ThreeOne),
            new RequirementDetail(DicomTag.OrderPlacerIdentifierSequence, RequirementCode.TwoTwo, GetHL7v2HierarchicDesignatorMacroForAddRequirements()),
            new RequirementDetail(DicomTag.FillerOrderNumberImagingServiceRequest, RequirementCode.ThreeOne),
            new RequirementDetail(DicomTag.OrderFillerIdentifierSequence, RequirementCode.TwoTwo, GetHL7v2HierarchicDesignatorMacroForAddRequirements()),
            new RequirementDetail(DicomTag.RequestedProcedureID, RequirementCode.TwoTwo),
            new RequirementDetail(DicomTag.RequestedProcedureDescription, RequirementCode.TwoTwo),
            new RequirementDetail(DicomTag.RequestedProcedureCodeSequence, RequirementCode.TwoTwo, GetUPSCodeSequenceMacroRequirements()),
            new RequirementDetail(DicomTag.ReasonForTheRequestedProcedure, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ReasonForRequestedProcedureCodeSequence, RequirementCode.ThreeThree, GetUPSCodeSequenceMacroRequirements()),
            new RequirementDetail(DicomTag.RequestedProcedureComments, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ConfidentialityCode, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.NamesOfIntendedRecipientsOfResults, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ImagingServiceRequestComments, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.RequestingPhysician, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.RequestingService, RequirementCode.ThreeOne),
            new RequirementDetail(DicomTag.RequestingServiceCodeSequence, RequirementCode.ThreeThree, GetUPSCodeSequenceMacroRequirements()),
            new RequirementDetail(DicomTag.IssueDateOfImagingServiceRequest, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.IssueTimeOfImagingServiceRequest, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ReferringPhysicianName, RequirementCode.ThreeThree),
        }));
        requirements.Add(new RequirementDetail(DicomTag.ReplacedProcedureStepSequence, requestType == WorkitemRequestType.Add ? RequirementCode.OneCOneC : RequirementCode.NotAllowed, new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.ReferencedSOPClassUID, RequirementCode.OneOne),
            new RequirementDetail(DicomTag.ReferencedSOPInstanceUID, RequirementCode.OneOne),
        }));
        requirements.Add(new RequirementDetail(DicomTag.TypeOfPatientID, RequirementCode.ThreeThree));
        requirements.Add(new RequirementDetail(DicomTag.PatientBirthDateInAlternativeCalendar, RequirementCode.ThreeThree));
        requirements.Add(new RequirementDetail(DicomTag.PatientDeathDateInAlternativeCalendar, RequirementCode.ThreeThree));
        requirements.Add(new RequirementDetail(DicomTag.PatientAlternativeCalendar, RequirementCode.ThreeThree));
        requirements.Add(new RequirementDetail(DicomTag.ReasonForVisit, RequirementCode.ThreeThree));
        requirements.Add(new RequirementDetail(DicomTag.ReasonForVisitCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()));

        return requirements;
    }

    /// <summary>
    /// Reference: https://dicom.nema.org/medical/dicom/current/output/html/part03.html#sect_C.2.3
    /// </summary>
    /// <returns>HashSet of requirements.</returns>
    private static HashSet<RequirementDetail> GetPatientDemographicModuleRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.PatientAge, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.Occupation, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ConfidentialityConstraintOnPatientDataDescription, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientBirthDate, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientBirthTime, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientSex, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.QualityControlSubject, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientInsurancePlanCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.PatientPrimaryLanguageCodeSequence, RequirementCode.ThreeThree, GetPrimaryLanguageCodeSequenceRequirements()),
            new RequirementDetail(DicomTag.PatientSize, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientWeight, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientSizeCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.PatientAddress, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.MilitaryRank, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.BranchOfService, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.CountryOfResidence, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.RegionOfResidence, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientTelephoneNumbers, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientTelecomInformation, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.EthnicGroup, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientReligiousPreference, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientComments, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ResponsiblePerson, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ResponsiblePersonRole, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ResponsibleOrganization, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientSpeciesDescription, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientSpeciesCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.PatientBreedDescription, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientBreedCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.BreedRegistrationSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.BreedRegistrationNumber, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.BreedRegistryCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            }),
            new RequirementDetail(DicomTag.StrainDescription, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.StrainNomenclature, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.StrainCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.StrainAdditionalInformation, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.StrainStockSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.StrainStockNumber, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.StrainSource, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.StrainSourceRegistryCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            }),
            new RequirementDetail(DicomTag.GeneticModificationsSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.GeneticModificationsDescription, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.GeneticModificationsNomenclature, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.GeneticModificationsCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            }),
        };
    }

    /// <summary>
    /// Reference: https://dicom.nema.org/medical/dicom/current/output/html/part03.html#sect_C.2.4
    /// </summary>
    /// <returns>HashSet of requirements.</returns>
    private static HashSet<RequirementDetail> GetPatientMedicalModuleRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.MedicalAlerts, RequirementCode.ThreeTwo),
            new RequirementDetail(DicomTag.PregnancyStatus, RequirementCode.ThreeTwo),
            new RequirementDetail(DicomTag.SpecialNeeds, RequirementCode.ThreeTwo),
            new RequirementDetail(DicomTag.Allergies, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.SmokingStatus, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.AdditionalPatientHistory, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.LastMenstrualDate, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientSexNeutered, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientBodyMassIndex, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.MeasuredAPDimension, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.MeasuredLateralDimension, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientState, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PertinentDocumentsSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.ReferencedSOPClassUID, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.ReferencedSOPInstanceUID, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.PurposeOfReferenceCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
                new RequirementDetail(DicomTag.DocumentTitle, RequirementCode.ThreeThree),
            }),
            new RequirementDetail(DicomTag.PertinentResourcesSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.RetrieveURI, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.ResourceDescription, RequirementCode.ThreeThree),
            }),
            new RequirementDetail(DicomTag.PatientClinicalTrialParticipationSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.ClinicalTrialSponsorName, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.ClinicalTrialProtocolID, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.ClinicalTrialProtocolName, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.ClinicalTrialSiteID, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.ClinicalTrialSiteName, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.ClinicalTrialSubjectID, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.ClinicalTrialSubjectReadingID, RequirementCode.ThreeThree),
            }),
        };
    }

    /// <summary>
    /// Reference: https://dicom.nema.org/medical/dicom/current/output/html/part03.html#sect_C.3.2
    /// </summary>
    /// <returns>HashSet of requirements.</returns>
    private static HashSet<RequirementDetail> GetVisitIdentificationModuleRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.InstitutionName, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.InstitutionAddress, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.InstitutionCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.InstitutionalDepartmentName, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.InstitutionalDepartmentTypeCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.AdmissionID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.IssuerOfAdmissionIDSequence, RequirementCode.ThreeThree, GetHL7v2HierarchicDesignatorMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.ReasonForVisit, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ReasonForVisitCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.ServiceEpisodeID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.IssuerOfServiceEpisodeIDSequence, RequirementCode.ThreeThree, GetHL7v2HierarchicDesignatorMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.ServiceEpisodeDescription, RequirementCode.ThreeThree),
        };
    }

    /// <summary>
    /// Reference: https://dicom.nema.org/medical/dicom/current/output/html/part03.html#sect_C.3.3
    /// </summary>
    /// <returns>HashSet of requirements.</returns>
    private static HashSet<RequirementDetail> GetVisitStatusModuleRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.VisitStatusID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.CurrentPatientLocation, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PatientInstitutionResidence, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.VisitComments, RequirementCode.ThreeThree),
        };
    }

    /// <summary>
    /// Reference: https://dicom.nema.org/medical/dicom/current/output/html/part03.html#sect_C.3.4
    /// </summary>
    /// <returns>HashSet of requirements.</returns>
    private static HashSet<RequirementDetail> GetVisitAdmissionModuleRequirements()
    {
        return new HashSet<RequirementDetail>()
        {
            new RequirementDetail(DicomTag.ReferringPhysicianName, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ReferringPhysicianAddress, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ReferringPhysicianTelephoneNumbers, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ReferringPhysicianIdentificationSequence, RequirementCode.ThreeThree, GetPersonIdentificationMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.ConsultingPhysicianName, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ConsultingPhysicianIdentificationSequence, RequirementCode.ThreeThree, GetPersonIdentificationMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.AdmittingDiagnosesDescription, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.AdmittingDiagnosesCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.RouteOfAdmissions, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.AdmittingDate, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.AdmittingTime, RequirementCode.ThreeThree),
        };
    }

    private static HashSet<RequirementDetail> GetUnifiedProcedureStepProgressInformationModuleRequirements(WorkitemRequestType requestType)
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.ProcedureStepState, requestType == WorkitemRequestType.Add ? RequirementCode.OneOne : RequirementCode.NotAllowed),
            new RequirementDetail(DicomTag.ProcedureStepProgressInformationSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.ThreeTwo, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.ProcedureStepProgress, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne),
                new RequirementDetail(DicomTag.ProcedureStepProgressDescription, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne),
                new RequirementDetail(DicomTag.ProcedureStepProgressParametersSequence, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeThree, GetProcedureStepProgressParameterSequenceRequirements(requestType)),
                new RequirementDetail(DicomTag.ProcedureStepCommunicationsURISequence, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne, new HashSet<RequirementDetail>
                {
                    new RequirementDetail(DicomTag.ContactURI, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.OneOne),
                    new RequirementDetail(DicomTag.ContactDisplayName, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne),
                }),
                new RequirementDetail(DicomTag.ProcedureStepCancellationDateTime, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne),
                new RequirementDetail(DicomTag.ReasonForCancellation, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne),
                new RequirementDetail(DicomTag.ProcedureStepDiscontinuationReasonCodeSequence, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne, GetUPSCodeSequenceMacroRequirements()),
            }),
        };
    }

    private static HashSet<RequirementDetail> GetUnifiedProcedureStepPerformedProcedureInformationModuleRequirements(WorkitemRequestType requestType)
    {
        HashSet<RequirementDetail> requirements = new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.UnifiedProcedureStepPerformedProcedureSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.ThreeTwo, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.ActualHumanPerformersSequence, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne, new HashSet<RequirementDetail>
                {
                    new RequirementDetail(DicomTag.HumanPerformerCodeSequence, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne, GetUPSCodeSequenceMacroRequirements()),
                    new RequirementDetail(DicomTag.HumanPerformerName, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne),
                    new RequirementDetail(DicomTag.HumanPerformerOrganization, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne),
                }),
                new RequirementDetail(DicomTag.PerformedStationNameCodeSequence, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeTwo, GetUPSCodeSequenceMacroRequirements()),
                new RequirementDetail(DicomTag.PerformedStationClassCodeSequence, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeTwo, GetUPSCodeSequenceMacroRequirements()),
                new RequirementDetail(DicomTag.PerformedStationGeographicLocationCodeSequence, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeTwo, GetUPSCodeSequenceMacroRequirements()),
                new RequirementDetail(DicomTag.PerformedProcedureStepStartDateTime, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne),
                new RequirementDetail(DicomTag.PerformedProcedureStepDescription, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne),
                new RequirementDetail(DicomTag.CommentsOnThePerformedProcedureStep, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne),
                new RequirementDetail(DicomTag.PerformedWorkitemCodeSequence, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne, GetUPSCodeSequenceMacroRequirements()),
                new RequirementDetail(DicomTag.PerformedProcessingParametersSequence, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne, GetUPSContentItemMacroRequirements()),
                new RequirementDetail(DicomTag.PerformedProcedureStepEndDateTime, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeOne),
                new RequirementDetail(DicomTag.OutputInformationSequence, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.TwoTwo, GetReferencedInstancesAndAccessMacroRequirements())
            }),
        };

        return requirements;
    }

    private static HashSet<RequirementDetail> GetCodeSequenceMacroAttributesRequirements()
    {
        HashSet<RequirementDetail> requirements = new HashSet<RequirementDetail>(GetBasicCodeSequenceMacroAttributesRequirements());
        requirements.Add(new RequirementDetail(DicomTag.EquivalentCodeSequence, RequirementCode.ThreeThree, GetEquivalentCodeSequenceRequirements()));
        requirements.UnionWith(GetEnhancedCodeSequenceMacroAttributesRequirements());
        return requirements;
    }

    private static HashSet<RequirementDetail> GetBasicCodeSequenceMacroAttributesRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.CodeValue, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.CodingSchemeDesignator, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.CodingSchemeVersion, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.CodeMeaning, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.LongCodeValue, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.URNCodeValue, RequirementCode.ThreeThree),
        };
    }

    private static HashSet<RequirementDetail> GetEnhancedCodeSequenceMacroAttributesRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.ContextIdentifier, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ContextUID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.MappingResource, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.MappingResourceUID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.MappingResourceName, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ContextGroupVersion, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ContextGroupExtensionFlag, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ContextGroupLocalVersion, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ContextGroupExtensionCreatorUID, RequirementCode.ThreeThree),
        };
    }

    private static HashSet<RequirementDetail> GetEquivalentCodeSequenceRequirements()
    {
        HashSet<RequirementDetail> requirements = new HashSet<RequirementDetail>(GetBasicCodeSequenceMacroAttributesRequirements());
        requirements.UnionWith(GetEnhancedCodeSequenceMacroAttributesRequirements());
        return requirements;
    }

    private static HashSet<RequirementDetail> GetPersonIdentificationMacroAttributesRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.PersonIdentificationCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.PersonAddress, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PersonTelephoneNumbers, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.PersonTelecomInformation, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.InstitutionName, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.InstitutionAddress, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.InstitutionCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            new RequirementDetail(DicomTag.InstitutionalDepartmentName, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.InstitutionalDepartmentTypeCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
        };
    }

    private static HashSet<RequirementDetail> GetDigitalSignatureMacroAttributesRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.MACParametersSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.MACIDNumber, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.MACCalculationTransferSyntaxUID, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.MACAlgorithm, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.DataElementsSigned, RequirementCode.ThreeThree),
            }),
            new RequirementDetail(DicomTag.DigitalSignaturesSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.MACIDNumber, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.DigitalSignatureUID, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.DigitalSignatureDateTime, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.CertificateType, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.CertificateOfSigner, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.Signature, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.CertifiedTimestampType, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.CertifiedTimestamp, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.DigitalSignaturePurposeCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()),
            }),
        };
    }

    private static RequirementDetail GetOriginalAttributesMacroAttributesRequirements()
    {
        return new RequirementDetail(DicomTag.OriginalAttributesSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.SourceOfPreviousValues, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.AttributeModificationDateTime, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ModifyingSystem, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ReasonForTheAttributeModification, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ModifiedAttributesSequence, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.NonconformingModifiedAttributesSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.SelectorAttribute, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.SelectorValueNumber, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.SelectorSequencePointer, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.SelectorSequencePointerPrivateCreator, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.SelectorSequencePointerItems, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.SelectorAttributePrivateCreator, RequirementCode.ThreeThree),
                new RequirementDetail(DicomTag.NonconformingDataElementValue, RequirementCode.ThreeThree),
            }),
        });
    }

    private static RequirementDetail GetHL7StructuredDocumentReferenceSequenceRequirements()
    {
        return new RequirementDetail(DicomTag.HL7StructuredDocumentReferenceSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.ReferencedSOPClassUID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ReferencedSOPInstanceUID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.HL7InstanceIdentifier, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.RetrieveURI, RequirementCode.ThreeThree),
        });
    }

    private static RequirementDetail GetConversionSourceAttributesSequenceRequirements()
    {
        return new RequirementDetail(DicomTag.ConversionSourceAttributesSequence, RequirementCode.ThreeThree, new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.ReferencedSOPClassUID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ReferencedSOPInstanceUID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ReferencedFrameNumber, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ReferencedSegmentNumber, RequirementCode.ThreeThree),
        });
    }

    private static HashSet<RequirementDetail> GetGeneralProcedureProtocolReferenceMacroAttributesRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.ReferencedDefinedProtocolSequence, RequirementCode.ThreeThree, GetReferencedProtocolSequenceRequirements()),
            new RequirementDetail(DicomTag.ReferencedPerformedProtocolSequence, RequirementCode.ThreeThree, GetReferencedProtocolSequenceRequirements()),
        };
    }

    private static HashSet<RequirementDetail> GetReferencedProtocolSequenceRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.ReferencedSOPClassUID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ReferencedSOPInstanceUID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.SourceAcquisitionProtocolElementNumber, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.SourceReconstructionProtocolElementNumber, RequirementCode.ThreeThree),
        };
    }

    private static HashSet<RequirementDetail> GetUPSContentItemMacroRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.ValueType, RequirementCode.OneOne),
            new RequirementDetail(DicomTag.ConceptNameCodeSequence, RequirementCode.OneOne, GetUPSCodeSequenceMacroRequirements()),
            new RequirementDetail(DicomTag.DateTime, RequirementCode.OneCOneC),
            new RequirementDetail(DicomTag.Date, RequirementCode.OneCOneC),
            new RequirementDetail(DicomTag.Time, RequirementCode.OneCOneC),
            new RequirementDetail(DicomTag.PersonName, RequirementCode.OneCOneC),
            new RequirementDetail(DicomTag.UID, RequirementCode.OneCOneC),
            new RequirementDetail(DicomTag.TextValue, RequirementCode.OneCOneC),
            new RequirementDetail(DicomTag.ConceptCodeSequence, RequirementCode.OneCOneC, GetUPSCodeSequenceMacroRequirements()),
            new RequirementDetail(DicomTag.NumericValue, RequirementCode.OneCOneC),
            new RequirementDetail(DicomTag.MeasurementUnitsCodeSequence, RequirementCode.OneCOneC, GetUPSCodeSequenceMacroRequirements()),
        };
    }

    /// <summary>
    /// https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-2a
    /// </summary>
    /// <returns></returns>
    private static HashSet<RequirementDetail> GetUPSCodeSequenceMacroRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.CodeValue, RequirementCode.OneCOneC),
            new RequirementDetail(DicomTag.CodingSchemeDesignator, RequirementCode.OneCOneC),
            new RequirementDetail(DicomTag.CodingSchemeVersion, RequirementCode.OneCOneC),
            new RequirementDetail(DicomTag.CodeMeaning, RequirementCode.OneOne),
            new RequirementDetail(DicomTag.LongCodeValue, RequirementCode.OneCOneC),
            new RequirementDetail(DicomTag.URNCodeValue, RequirementCode.OneCOneC),
            new RequirementDetail(DicomTag.MappingResource, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.MappingResourceUID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ContextGroupVersion, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ContextGroupExtensionFlag, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ContextGroupLocalVersion, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.ContextGroupExtensionCreatorUID, RequirementCode.ThreeThree),
        };
    }

    private static HashSet<RequirementDetail> GetReferencedInstancesAndAccessMacroRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.TypeOfInstances, RequirementCode.OneOne),
            new RequirementDetail(DicomTag.StudyInstanceUID, RequirementCode.OneCOne),
            new RequirementDetail(DicomTag.SeriesInstanceUID, RequirementCode.OneCOne),
            new RequirementDetail(DicomTag.ReferencedSOPSequence, RequirementCode.OneOne, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.ReferencedSOPClassUID, RequirementCode.OneOne),
                new RequirementDetail(DicomTag.ReferencedSOPInstanceUID, RequirementCode.OneOne),
                new RequirementDetail(DicomTag.HL7InstanceIdentifier, RequirementCode.OneCOne),
                new RequirementDetail(DicomTag.ReferencedFrameNumber, RequirementCode.OneCOne),
                new RequirementDetail(DicomTag.ReferencedSegmentNumber, RequirementCode.OneCOne),
            }),
            new RequirementDetail(DicomTag.DICOMRetrievalSequence, RequirementCode.OneCOne, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.RetrieveAETitle, RequirementCode.OneOne),
            }),
            new RequirementDetail(DicomTag.DICOMMediaRetrievalSequence, RequirementCode.OneCOne, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.StorageMediaFileSetID, RequirementCode.TwoTwo),
                new RequirementDetail(DicomTag.StorageMediaFileSetUID, RequirementCode.OneOne),
            }),
            new RequirementDetail(DicomTag.WADORetrievalSequence, RequirementCode.OneCOne, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.RetrieveURI, RequirementCode.OneOne),
            }),
            new RequirementDetail(DicomTag.XDSRetrievalSequence, RequirementCode.OneCOne, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.RepositoryUniqueID, RequirementCode.OneOne),
                new RequirementDetail(DicomTag.HomeCommunityID, RequirementCode.ThreeTwo),
            }),
            new RequirementDetail(DicomTag.WADORSRetrievalSequence, RequirementCode.OneCOne, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.RetrieveURL, RequirementCode.OneOne),
            }),
        };
    }

    private static HashSet<RequirementDetail> GetOtherPatientIDSequenceRequirements(WorkitemRequestType requestType)
    {
        HashSet<RequirementDetail> requirements = new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.PatientID, RequirementCode.OneOne),
        };

        requirements.UnionWith(GetIssuerOfPatientIDMacroRequirements(requestType));

        requirements.Add(new RequirementDetail(DicomTag.TypeOfPatientID, RequirementCode.ThreeThree));

        return requirements;
    }

    /// <summary>
    /// Reference: https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-2e
    /// </summary>
    /// <param name="requestType"><see cref="WorkitemRequestType"/></param>
    /// <returns>HashSet of requirement detail.</returns>
    private static HashSet<RequirementDetail> GetIssuerOfPatientIDMacroRequirements(WorkitemRequestType requestType)
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.IssuerOfPatientID, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.NotAllowed),
            new RequirementDetail(DicomTag.IssuerOfPatientIDQualifiersSequence, requestType == WorkitemRequestType.Add ? RequirementCode.TwoTwo : RequirementCode.NotAllowed, new HashSet<RequirementDetail>
            {
                new RequirementDetail(DicomTag.UniversalEntityID, RequirementCode.TwoTwo),
                new RequirementDetail(DicomTag.UniversalEntityIDType, RequirementCode.OneCOne),
                new RequirementDetail(DicomTag.IdentifierTypeCode, RequirementCode.TwoTwo),
                new RequirementDetail(DicomTag.AssigningFacilitySequence, RequirementCode.TwoTwo, GetHL7v2HierarchicDesignatorMacroForAddRequirements()),
                new RequirementDetail(DicomTag.AssigningJurisdictionCodeSequence, RequirementCode.TwoTwo, GetUPSCodeSequenceMacroRequirements()),
                new RequirementDetail(DicomTag.AssigningAgencyOrDepartmentCodeSequence, RequirementCode.TwoTwo, GetUPSCodeSequenceMacroRequirements()),
            }),
        };
    }

    private static HashSet<RequirementDetail> GetPrimaryLanguageCodeSequenceRequirements()
    {
        HashSet<RequirementDetail> requirements = new HashSet<RequirementDetail>(GetCodeSequenceMacroAttributesRequirements());
        requirements.Add(new RequirementDetail(DicomTag.PatientPrimaryLanguageModifierCodeSequence, RequirementCode.ThreeThree, GetCodeSequenceMacroAttributesRequirements()));
        return requirements;
    }

    /// <summary>
    /// Reference: https://dicom.nema.org/medical/dicom/current/output/html/part03.html#table_10-17
    /// </summary>
    /// <returns>HashSet of requirement details.</returns>
    private static HashSet<RequirementDetail> GetHL7v2HierarchicDesignatorMacroAttributesRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.LocalNamespaceEntityID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.UniversalEntityID, RequirementCode.ThreeThree),
            new RequirementDetail(DicomTag.UniversalEntityIDType, RequirementCode.ThreeThree),
        };
    }

    private static HashSet<RequirementDetail> GetProcedureStepProgressParameterSequenceRequirements(WorkitemRequestType requestType)
    {
        HashSet<RequirementDetail> requirements = new HashSet<RequirementDetail>(GetUPSContentItemMacroRequirements());
        requirements.Add(new RequirementDetail(DicomTag.ContentItemModifierSequence, requestType == WorkitemRequestType.Add ? RequirementCode.NotAllowed : RequirementCode.ThreeThree, GetUPSContentItemMacroRequirements()));
        return requirements;
    }

    /// <summary>
    /// Reference: https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-2d
    /// </summary>
    /// <returns>Hashset of requirement details.</returns>
    private static HashSet<RequirementDetail> GetHL7v2HierarchicDesignatorMacroForAddRequirements()
    {
        return new HashSet<RequirementDetail>
        {
            new RequirementDetail(DicomTag.LocalNamespaceEntityID, RequirementCode.OneCOne),
            new RequirementDetail(DicomTag.UniversalEntityID, RequirementCode.OneCOne),
            new RequirementDetail(DicomTag.UniversalEntityIDType, RequirementCode.OneCOne),
        };
    }
}
