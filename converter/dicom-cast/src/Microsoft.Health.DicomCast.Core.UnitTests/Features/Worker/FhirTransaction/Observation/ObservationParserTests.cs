// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
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
        public void DoseSummaryWithAllSupportedAttributes()
        {
            const string studyInstanceUid = "1.3.12.2.123.5.4.5.123123.123123";
            const string accessionNumber = "random-accession";
            const decimal randomDecimalNumber = (decimal)0.10;
            var randomRadiationMeasurementCodeItem = new DicomCodeItem("mGy", "UCUM", "mGy");

            var report = new DicomStructuredReport(
                ObservationConstants.RadiopharmaceuticalRadiationDoseReport,
                // identifiers
                new DicomContentItem(
                    ObservationConstants.StudyInstanceUid,
                    DicomRelationship.HasProperties,
                    new DicomUID(studyInstanceUid, "", DicomUidType.Unknown)),
                new DicomContentItem(
                    ObservationConstants.AccessionNumber,
                    DicomRelationship.HasProperties,
                    DicomValueType.Text,
                    accessionNumber),

                // attributes
                new DicomContentItem(
                    ObservationConstants.EntranceExposureAtRp,
                    DicomRelationship.Contains,
                    new DicomMeasuredValue(randomDecimalNumber,
                        randomRadiationMeasurementCodeItem)),
                new DicomContentItem(
                    ObservationConstants.AccumulatedAverageGlandularDose,
                    DicomRelationship.Contains,
                    new DicomMeasuredValue(randomDecimalNumber,
                        randomRadiationMeasurementCodeItem)),
                new DicomContentItem(
                    ObservationConstants.DoseAreaProductTotal,
                    DicomRelationship.Contains,
                    new DicomMeasuredValue(randomDecimalNumber,
                        randomRadiationMeasurementCodeItem)),
                new DicomContentItem(
                    ObservationConstants.FluoroDoseAreaProductTotal,
                    DicomRelationship.Contains,
                    new DicomMeasuredValue(randomDecimalNumber,
                        randomRadiationMeasurementCodeItem)),
                new DicomContentItem(
                    ObservationConstants.AcquisitionDoseAreaProductTotal,
                    DicomRelationship.Contains,
                    new DicomMeasuredValue(randomDecimalNumber,
                        randomRadiationMeasurementCodeItem)),
                new DicomContentItem(
                    ObservationConstants.TotalFluoroTime,
                    DicomRelationship.Contains,
                    new DicomMeasuredValue(randomDecimalNumber,
                        randomRadiationMeasurementCodeItem)),
                new DicomContentItem(
                    ObservationConstants.TotalNumberOfRadiographicFrames,
                    DicomRelationship.Contains,
                    new DicomMeasuredValue(10,
                        new DicomCodeItem("1", "UCUM", "No units"))),
                new DicomContentItem(
                    ObservationConstants.AdministeredActivity,
                    DicomRelationship.Contains,
                    new DicomMeasuredValue(randomDecimalNumber,
                        randomRadiationMeasurementCodeItem)),
                new DicomContentItem(
                    ObservationConstants.CtDoseLengthProductTotal,
                    DicomRelationship.Contains,
                    new DicomMeasuredValue(randomDecimalNumber,
                        randomRadiationMeasurementCodeItem)),
                new DicomContentItem(
                    ObservationConstants.TotalNumberOfIrradiationEvents,
                    DicomRelationship.Contains,
                    new DicomMeasuredValue(10,
                        new DicomCodeItem("1", "UCUM", "No units"))),
                new DicomContentItem(
                    ObservationConstants.MeanCtdIvol,
                    DicomRelationship.Contains,
                    new DicomMeasuredValue(randomDecimalNumber,
                        randomRadiationMeasurementCodeItem)),
                new DicomContentItem(
                    ObservationConstants.RadiopharmaceuticalAgent,
                    DicomRelationship.Contains,
                    DicomValueType.Text,
                    "Uranium"),
                new DicomContentItem(
                    ObservationConstants.Radionuclide,
                    DicomRelationship.Contains,
                    DicomValueType.Text,
                    "Uranium"),
                new DicomContentItem(
                    ObservationConstants.RadiopharmaceuticalVolume,
                    DicomRelationship.Contains,
                    new DicomMeasuredValue(randomDecimalNumber,
                        randomRadiationMeasurementCodeItem)),
                new DicomContentItem(
                    ObservationConstants.RouteOfAdministration,
                    DicomRelationship.Contains,
                    new DicomCodeItem("needle", "random-scheme", "this is made up"))
            );

            ParsedObservation observations = ObservationParser.CreateObservations(
                report.Dataset,
                new ResourceReference(),
                new ResourceReference());
            Assert.Single(observations.DoseSummaries);

            Observation doseSummary = observations.DoseSummaries.First();
            Assert.Equal(2, doseSummary.Identifier.Count);
            Assert.Equal("urn:oid:" + studyInstanceUid,
                doseSummary.Identifier[0].Value);
            Assert.Equal(accessionNumber,
                doseSummary.Identifier[1].Value);
            Assert.Equal(10,
                doseSummary.Component
                    .Count(component => component.Value is Quantity));
            Assert.Equal(2,
                doseSummary.Component
                    .Count(component => component.Value is Integer));
            Assert.Equal(2,
                doseSummary.Component
                    .Count(component => component.Value is FhirString));
            Assert.Equal(1,
                doseSummary.Component
                    .Count(component => component.Value is CodeableConcept));
        }

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
                    ObservationConstants.StudyInstanceUid,
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
            var report = new DicomStructuredReport(
                ObservationConstants.IrradiationEventXRayData);

            ParsedObservation observations = ObservationParser.CreateObservations(
                report.Dataset,
                new ResourceReference(),
                new ResourceReference());
            Assert.Empty(observations.DoseSummaries);
            Assert.Empty(observations.IrradiationEvents);
        }
    }
}
