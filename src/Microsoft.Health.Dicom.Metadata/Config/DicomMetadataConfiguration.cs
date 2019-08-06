// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Metadata.Config
{
    public class DicomMetadataConfiguration
    {
        public HashSet<DicomAttributeId> StudyRequiredMetadataAttributes { get; } = new HashSet<DicomAttributeId>()
        {
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
        };

        public HashSet<DicomAttributeId> StudyOptionalMetadataAttributes { get; } = new HashSet<DicomAttributeId>()
        {
            new DicomAttributeId(DicomTag.PersonIdentificationCodeSequence),
            new DicomAttributeId(DicomTag.PersonAddress),
            new DicomAttributeId(DicomTag.PersonTelephoneNumbers),
            new DicomAttributeId(DicomTag.PersonTelecomInformation),
            new DicomAttributeId(DicomTag.InstitutionName),
            new DicomAttributeId(DicomTag.InstitutionAddress),
            new DicomAttributeId(DicomTag.InstitutionCodeSequence),
            new DicomAttributeId(DicomTag.ReferringPhysicianIdentificationSequence),
            new DicomAttributeId(DicomTag.ConsultingPhysicianName),
            new DicomAttributeId(DicomTag.ConsultingPhysicianIdentificationSequence),
            new DicomAttributeId(DicomTag.IssuerOfAccessionNumberSequence),
            new DicomAttributeId(DicomTag.LocalNamespaceEntityID),
            new DicomAttributeId(DicomTag.UniversalEntityID),
            new DicomAttributeId(DicomTag.UniversalEntityIDType),
            new DicomAttributeId(DicomTag.StudyDescription),
            new DicomAttributeId(DicomTag.PhysiciansOfRecord),
            new DicomAttributeId(DicomTag.PhysiciansOfRecordIdentificationSequence),
            new DicomAttributeId(DicomTag.NameOfPhysiciansReadingStudy),
            new DicomAttributeId(DicomTag.PhysiciansReadingStudyIdentificationSequence),
            new DicomAttributeId(DicomTag.RequestingServiceCodeSequence),
            new DicomAttributeId(DicomTag.ReferencedStudySequence),
            new DicomAttributeId(DicomTag.ProcedureCodeSequence),
            new DicomAttributeId(DicomTag.ReasonForPerformedProcedureCodeSequence),
        };

        public HashSet<DicomAttributeId> SeriesRequiredMetadataAttributes { get; } = new HashSet<DicomAttributeId>()
        {
            new DicomAttributeId(DicomTag.SpecificCharacterSet),
            new DicomAttributeId(DicomTag.Modality),
            new DicomAttributeId(DicomTag.TimezoneOffsetFromUTC),
            new DicomAttributeId(DicomTag.SeriesDescription),
            new DicomAttributeId(DicomTag.PerformedProcedureStepStartDate),
            new DicomAttributeId(DicomTag.PerformedProcedureStepStartTime),
            new DicomAttributeId(DicomTag.RequestAttributesSequence),
            new DicomAttributeId(DicomTag.BitsAllocated),
            new DicomAttributeId(DicomTag.TransferSyntaxUID),
        };

        public HashSet<DicomAttributeId> SeriesOptionalMetadataAttributes { get; } = new HashSet<DicomAttributeId>()
        {
            new DicomAttributeId(DicomTag.SeriesNumber),
            new DicomAttributeId(DicomTag.Laterality),
            new DicomAttributeId(DicomTag.SeriesDate),
            new DicomAttributeId(DicomTag.SeriesTime),
        };

        public HashSet<DicomAttributeId> InstanceRequiredMetadataAttributes { get; } = new HashSet<DicomAttributeId>()
        {
            new DicomAttributeId(DicomTag.SpecificCharacterSet),
            new DicomAttributeId(DicomTag.SOPClassUID),
            new DicomAttributeId(DicomTag.TimezoneOffsetFromUTC),
            new DicomAttributeId(DicomTag.InstanceNumber),
            new DicomAttributeId(DicomTag.Rows),
            new DicomAttributeId(DicomTag.Columns),
            new DicomAttributeId(DicomTag.BitsAllocated),
            new DicomAttributeId(DicomTag.NumberOfFrames),
        };

        public HashSet<DicomAttributeId> StudySeriesMetadataAttributes => new HashSet<DicomAttributeId>(
                                                StudyRequiredMetadataAttributes
                                                    .Concat(StudyOptionalMetadataAttributes)
                                                    .Concat(SeriesRequiredMetadataAttributes)
                                                    .Concat(SeriesOptionalMetadataAttributes));
    }
}
