// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class FhirTransactionContextBuilder
    {
        public static readonly DateTime DefaultStudyDateTime = new DateTime(1974, 7, 10, 7, 10, 24);
        public static readonly DateTime DefaultSeriesDateTime = new DateTime(1974, 8, 10, 8, 10, 24);
        public const string DefaultSOPClassUID = "4444";
        public const string DefaultStudyDescription = "Study Description";
        public const string DefaultSeriesDescription = "Series Description";
        public const string DefaultModalitiesInStudy = "MODALITY";
        public const string DefaultModality = "MODALITY";
        public const string DefaultSeriesNumber = "1";
        public const string DefaultInstanceNumber = "1";
        public const string DefaultAccessionNumber = "1";

        public static DicomDataset CreateDicomDataset(string sopClassUid = null, string studyDescription = null, string seriesDescrition = null, string modalityInStudy = null, string modalityInSeries = null, string seriesNumber = null, string instanceNumber = null, string accessionNumber = null)
        {
            var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
                {
                    { DicomTag.SOPClassUID, sopClassUid ?? DefaultSOPClassUID },
                    { DicomTag.StudyDate, DefaultStudyDateTime },
                    { DicomTag.StudyTime, DefaultStudyDateTime },
                    { DicomTag.SeriesDate, DefaultSeriesDateTime },
                    { DicomTag.SeriesTime, DefaultSeriesDateTime },
                    { DicomTag.StudyDescription, studyDescription ?? DefaultStudyDescription },
                    { DicomTag.SeriesDescription, seriesDescrition ?? DefaultSeriesDescription },
                    { DicomTag.ModalitiesInStudy, modalityInStudy ?? DefaultModalitiesInStudy },
                    { DicomTag.Modality, modalityInSeries ?? DefaultModality },
                    { DicomTag.SeriesNumber, seriesNumber ?? DefaultSeriesNumber },
                    { DicomTag.InstanceNumber, instanceNumber ?? DefaultInstanceNumber },
                    { DicomTag.AccessionNumber, accessionNumber ?? DefaultAccessionNumber },
                };

            return ds;
        }

        public static FhirTransactionContext DefaultFhirTransactionContext(DicomDataset metadata = null)
        {
            var context = new FhirTransactionContext(ChangeFeedGenerator.Generate(metadata: metadata ?? CreateDicomDataset()))
            {
                UtcDateTimeOffset = TimeSpan.Zero,
            };

            context.Request.Patient = FhirTransactionRequestEntryGenerator.GenerateDefaultCreateRequestEntry<Patient>();
            context.Request.ImagingStudy = FhirTransactionRequestEntryGenerator.GenerateDefaultCreateRequestEntry<ImagingStudy>();
            context.Request.Endpoint = FhirTransactionRequestEntryGenerator.GenerateDefaultCreateRequestEntry<Endpoint>();

            return context;
        }
    }
}
