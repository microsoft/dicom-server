// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Dicom.StructuredReport;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class ObservationParserTests
    {
        private static DicomDataset DoseSummaryWithStudyInstanceUidInTag()
        {
            var ds = new DicomDataset();
            ds.Add(DicomTag.StudyInstanceUID, "12345");
            ds.Add(DicomTag.AccessionNumber, "12345");
            ds.Add(DicomTag.ConceptNameCodeSequence,
                new DicomContentItem(
                    new DicomCodeItem(
                        ObservationConstants.XRayRadiationDoseReport.Item1,
                        ObservationConstants.XRayRadiationDoseReport.Item2,
                        "X-Ray Radiation Dose Report")));
            return ds;
        }


        private static DicomDataset DoseSummaryWithStudyInstanceUidInReport()
        {
            var report = new DicomContentItem(
                new DicomCodeItem(
                    ObservationConstants.XRayRadiationDoseReport.Item1,
                    ObservationConstants.XRayRadiationDoseReport.Item2,
                    "X-Ray Radiation Dose Report"));
            report.Add(new DicomContentItem(
                new DicomCodeItem(
                    ObservationConstants.StudyInstanceUid.Item1,
                    ObservationConstants.StudyInstanceUid.Item2,
                    "Study Instance UID"),
                DicomRelationship.HasProperties,
                new DicomUID("1.3.12.2.123.5.4.5.123123.123123", "", DicomUidType.Unknown)));

            return report.Dataset;
        }

        private static DicomDataset RadiationEventWithIrradiationEventUid()
        {
            var ds = new DicomDataset();
            var contentItem = new DicomContentItem(ds);
            contentItem.Add(
                new DicomCodeItem(
                    ObservationConstants.IrradiationEventXRayData.Item1,
                    ObservationConstants.IrradiationEventXRayData.Item2,
                    "Irradiation Event X-Ray Data"),
                DicomRelationship.Contains,
                DicomContinuity.Separate,
                new DicomContentItem(
                    new DicomCodeItem(
                        ObservationConstants.IrradiationEventUid.Item1,
                        ObservationConstants.IrradiationEventUid.Item2,
                        "Irradiation Event UID"),
                    DicomRelationship.Contains,
                    new DicomUID("1.3.12.2.1234.5.4.5.123123.3000000111", "foobar", DicomUidType.Unknown)
                ));

            return contentItem.Dataset;
        }

        private static DicomDataset RadiationEventWithoutIrradiationEventUid()
        {
            var ds = new DicomDataset();
            var contentItem = new DicomContentItem(ds);
            contentItem.Add(
                new DicomCodeItem(
                    ObservationConstants.IrradiationEventXRayData.Item1,
                    ObservationConstants.IrradiationEventXRayData.Item2,
                    "Irradiation Event X-Ray Data"),
                DicomRelationship.Contains,
                DicomContinuity.Separate);
            return contentItem.Dataset;
        }

        [Fact]
        public void CreateObservations_Tests()
        {
            // Tuple of Dataset to number of expected observations to be found
            (DicomDataset, int)[] tests =
            {
                (new DicomDataset(), 0),
                (DoseSummaryWithStudyInstanceUidInTag(), 1),
                (DoseSummaryWithStudyInstanceUidInTag().Remove(DicomTag.StudyInstanceUID), 0), // studyInstanceUID is required
                (DoseSummaryWithStudyInstanceUidInTag().Remove(DicomTag.AccessionNumber), 1), // accession number is optional
                (DoseSummaryWithStudyInstanceUidInReport(), 1),
                (RadiationEventWithIrradiationEventUid(), 1),
                (RadiationEventWithoutIrradiationEventUid(), 0)
            };
            foreach ((DicomDataset dicomDataset, int expectedNumberOfObservations) in tests)
            {
                ParsedObservation observations = ObservationParser.CreateObservations(
                    dicomDataset,
                    new ResourceReference(),
                    new ResourceReference());
                Assert.Equal(expectedNumberOfObservations, observations.DoseSummaries.Count + observations.IrradiationEvents.Count);
            }

            Assert.True(true);
        }
    }
}
