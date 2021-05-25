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
        [Fact]
        public void DoseSummaryWithStudyInstanceUidInTag()
        {
            var report = new DicomStructuredReport(
                ObservationConstants.XRayRadiationDoseReport);
            report.Dataset
                .Add(DicomTag.StudyInstanceUID, "12345")
                .Add(DicomTag.AccessionNumber, "12345");

            ParsedObservation observations = ObservationParser.CreateObservations(
                report.Dataset,
                new ResourceReference(),
                new ResourceReference());
            Assert.Single(observations.DoseSummaries);
            Assert.Empty(observations.IrradiationEvents);
        }


        [Fact]
        public void DoseSummaryWithStudyInstanceUidInReport()
        {
            var report = new DicomStructuredReport(
                ObservationConstants.XRayRadiationDoseReport,
                new DicomContentItem(
                    new DicomCodeItem(ObservationConstants.StudyInstanceUid),
                    DicomRelationship.HasProperties,
                    new DicomUID("1.3.12.2.123.5.4.5.123123.123123", "", DicomUidType.Unknown)));

            ParsedObservation observations = ObservationParser.CreateObservations(
                report.Dataset,
                new ResourceReference(),
                new ResourceReference());
            Assert.Single(observations.DoseSummaries);
            Assert.Empty(observations.IrradiationEvents);
        }

        [Fact]
        public void RadiationEventWithIrradiationEventUid()
        {
            var report = new DicomStructuredReport(
                ObservationConstants.IrradiationEventXRayData,
                new DicomContentItem(
                    ObservationConstants.IrradiationEventUid,
                    DicomRelationship.Contains,
                    new DicomUID("1.3.12.2.1234.5.4.5.123123.3000000111", "foobar", DicomUidType.Unknown)
                ));

            ParsedObservation observations = ObservationParser.CreateObservations(
                report.Dataset,
                new ResourceReference(),
                new ResourceReference());
            Assert.Empty(observations.DoseSummaries);
            Assert.Single(observations.IrradiationEvents);
        }

        [Fact]
        public void RadiationEventWithoutIrradiationEventUid()
        {
            var ds = new DicomDataset();
            var contentItem = new DicomContentItem(ds);
            contentItem.Add(
                ObservationConstants.IrradiationEventXRayData,
                DicomRelationship.Contains,
                DicomContinuity.Separate);

            ParsedObservation observations = ObservationParser.CreateObservations(
                ds,
                new ResourceReference(),
                new ResourceReference());
            Assert.Empty(observations.DoseSummaries);
            Assert.Empty(observations.IrradiationEvents);
        }
    }
}
