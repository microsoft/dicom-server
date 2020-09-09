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
        private static DateTime studyDateTime = new DateTime(1974, 7, 10, 7, 10, 24);
        private static DateTime seriesDateTime = new DateTime(1974, 8, 10, 8, 10, 24);

        public static DicomDataset CreateDicomDataset(string sopClassUid = null, string studyDescription = null, string seriesDescrition = null, string modalityInStudy = null, string modalityInSeries = null, string seriesNumber = null, string instanceNumber = null)
        {
            var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
                {
                    { DicomTag.SOPClassUID, sopClassUid ?? "4444" },
                    { DicomTag.StudyDate, studyDateTime },
                    { DicomTag.StudyTime, studyDateTime },
                    { DicomTag.SeriesDate, seriesDateTime },
                    { DicomTag.SeriesTime, seriesDateTime },
                    { DicomTag.StudyDescription, studyDescription ?? "Study Description" },
                    { DicomTag.SeriesDescription, seriesDescrition ?? "Series Description" },
                    { DicomTag.ModalitiesInStudy, modalityInStudy ?? "MODALITY" },
                    { DicomTag.Modality, modalityInSeries ?? "MODALITY" },
                    { DicomTag.SeriesNumber, seriesNumber ?? "1" },
                    { DicomTag.InstanceNumber, instanceNumber ?? "1" },
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
