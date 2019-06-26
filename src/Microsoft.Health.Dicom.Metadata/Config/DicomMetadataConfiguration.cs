// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Metadata.Config
{
    public class DicomMetadataConfiguration
    {
        /// <summary>
        /// Gets the DICOM tags that should be stored for resolving metadata responses.
        /// The StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID will be indexed automatically.
        /// TODO: Handle Sequences?
        /// </summary>
        public HashSet<DicomAttributeId> MetadataAttributes { get; } = new HashSet<DicomAttributeId>()
        {
            // Study DICOM Tags
            new DicomAttributeId(DicomTag.SpecificCharacterSet),
            new DicomAttributeId(DicomTag.StudyDate),
            new DicomAttributeId(DicomTag.StudyTime),
            new DicomAttributeId(DicomTag.AccessionNumber),
            new DicomAttributeId(DicomTag.ReferringPhysicianName),
            new DicomAttributeId(DicomTag.TimezoneOffsetFromUTC),
            new DicomAttributeId(DicomTag.PatientName),
            new DicomAttributeId(DicomTag.PatientID),
            new DicomAttributeId(DicomTag.PatientBirthDate),
            new DicomAttributeId(DicomTag.PatientSex),
            new DicomAttributeId(DicomTag.StudyID),

            // new DicomAttributeId(DicomTag.PersonIdentificationCodeSequence),
            new DicomAttributeId(DicomTag.PersonAddress),
            new DicomAttributeId(DicomTag.PersonTelephoneNumbers),
            new DicomAttributeId(DicomTag.PersonTelecomInformation),
            new DicomAttributeId(DicomTag.InstitutionName),
            new DicomAttributeId(DicomTag.InstitutionAddress),

            // new DicomAttributeId(DicomTag.InstitutionCodeSequence),
            // new DicomAttributeId(DicomTag.ReferringPhysicianIdentificationSequence),
            new DicomAttributeId(DicomTag.ConsultingPhysicianName),

            // new DicomAttributeId(DicomTag.ConsultingPhysicianIdentificationSequence),
            // new DicomAttributeId(DicomTag.IssuerOfAccessionNumberSequence),
            new DicomAttributeId(DicomTag.LocalNamespaceEntityID),
            new DicomAttributeId(DicomTag.UniversalEntityID),
            new DicomAttributeId(DicomTag.UniversalEntityIDType),
            new DicomAttributeId(DicomTag.StudyDescription),
            new DicomAttributeId(DicomTag.PhysiciansOfRecord),

            // new DicomAttributeId(DicomTag.PhysiciansOfRecordIdentificationSequence),
            new DicomAttributeId(DicomTag.NameOfPhysiciansReadingStudy),

            // new DicomAttributeId(DicomTag.PhysiciansReadingStudyIdentificationSequence),
            // new DicomAttributeId(DicomTag.RequestingServiceCodeSequence),
            // new DicomAttributeId(DicomTag.ReferencedStudySequence),
            // new DicomAttributeId(DicomTag.ProcedureCodeSequence),
            // new DicomAttributeId(DicomTag.ReasonForPerformedProcedureCodeSequence),
            // Series DICOM Tags
            new DicomAttributeId(DicomTag.SpecificCharacterSet),
            new DicomAttributeId(DicomTag.Modality),
            new DicomAttributeId(DicomTag.TimezoneOffsetFromUTC),
            new DicomAttributeId(DicomTag.SeriesDescription),
            new DicomAttributeId(DicomTag.PerformedProcedureStepStartDate),
            new DicomAttributeId(DicomTag.PerformedProcedureStepStartTime),

            // new DicomAttributeId(DicomTag.RequestAttributesSequence),
            new DicomAttributeId(DicomTag.SeriesNumber),
            new DicomAttributeId(DicomTag.Laterality),
            new DicomAttributeId(DicomTag.SeriesDate),
            new DicomAttributeId(DicomTag.SeriesTime),
        };
    }
}
