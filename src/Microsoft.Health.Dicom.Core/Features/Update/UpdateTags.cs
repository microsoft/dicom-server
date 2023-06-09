// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Update;

internal static class UpdateTags
{
    internal const int MaxStudyInstanceUidLimit = 50;

    internal static readonly HashSet<DicomTag> UpdateStudyFilterTags = new HashSet<DicomTag>()
    {
        DicomTag.PatientName,
        DicomTag.PatientID,
        DicomTag.OtherPatientIDsRETIRED,
        DicomTag.TypeOfPatientID,
        DicomTag.OtherPatientNames,
        DicomTag.PatientBirthName,
        DicomTag.PatientMotherBirthName,
        DicomTag.MedicalRecordLocatorRETIRED,
        DicomTag.PatientAge,
        DicomTag.Occupation,
        DicomTag.ConfidentialityConstraintOnPatientDataDescription,
        DicomTag.PatientBirthDate,
        DicomTag.PatientBirthTime,
        DicomTag.PatientSex,
        DicomTag.QualityControlSubject,
        DicomTag.PatientSize,
        DicomTag.PatientWeight,
        DicomTag.PatientAddress,
        DicomTag.MilitaryRank,
        DicomTag.BranchOfService,
        DicomTag.CountryOfResidence,
        DicomTag.RegionOfResidence,
        DicomTag.PatientTelephoneNumbers,
        DicomTag.EthnicGroup,
        DicomTag.PatientReligiousPreference,
        DicomTag.PatientComments,
        DicomTag.ResponsiblePerson,
        DicomTag.ResponsiblePersonRole,
        DicomTag.ResponsibleOrganization,
        DicomTag.PatientSpeciesDescription,
        DicomTag.PatientBreedDescription,
        DicomTag.BreedRegistrationNumber,
    };
}
