// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class ImagingStudyInstancePropertySynchronizerTests
    {
        private readonly IImagingStudyInstancePropertySynchronizer _imagingStudyInstancePropertySynchronizer;
        private readonly string studyInstanceUid = "111";
        private readonly string seriesInstanceUid = "222";
        private readonly string sopInstanceUid = "333";
        private readonly string sopClassUid = "4444";
        private readonly string patientResourceId = "555";

        public ImagingStudyInstancePropertySynchronizerTests()
        {
            _imagingStudyInstancePropertySynchronizer = new ImagingStudyInstancePropertySynchronizer();
        }

        [Fact]
        public void GivenATransactionContexAndImagingStudy_WhenprocessedForInstance_ThenDicomPropertiesAreCorrectlyMappedtoInstanceWithinImagingStudy()
        {
            DicomDataset dataset = FhirTransactionContextBuilder.CreateDicomDataset();

            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(dataset);

            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();
            ImagingStudy.InstanceComponent instance = series.Instance.First();

            _imagingStudyInstancePropertySynchronizer.Synchronize(context, instance);

            Assert.Equal(sopClassUid, instance.SopClass.Code);
            Assert.Equal(1, instance.Number);
        }

        [Fact]
        public void GivenATransactionContextWithUpdatedInstanceNumber_WhenprocessedForInstance_ThenDicomPropertyValuesAreUpdatedCorrectly()
        {
            DicomDataset dataset = FhirTransactionContextBuilder.CreateDicomDataset();

            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(dataset);

            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();
            ImagingStudy.InstanceComponent instance = series.Instance.First();

            _imagingStudyInstancePropertySynchronizer.Synchronize(context, instance);

            Assert.Equal(1, instance.Number);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(instanceNumber: "2"));

            _imagingStudyInstancePropertySynchronizer.Synchronize(newContext, instance);
            Assert.Equal(2, instance.Number);
        }

        [Fact]
        public void GivenATransactionContextWithNoDicomPropertyValueChange_WhenprocessedForInstancee_ThenDicomPropertyValuesUpdateIsSkipped()
        {
            DicomDataset dataset = FhirTransactionContextBuilder.CreateDicomDataset();

            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(dataset);

            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();
            ImagingStudy.InstanceComponent instance = series.Instance.First();

            _imagingStudyInstancePropertySynchronizer.Synchronize(context, instance);

            Assert.Equal(1, instance.Number);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext(dataset);

            _imagingStudyInstancePropertySynchronizer.Synchronize(newContext, instance);
            Assert.Equal(1, instance.Number);
        }
    }
}
