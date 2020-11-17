// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class ImagingStudySeriesPropertySynchronizerTests
    {
        private readonly IImagingStudySeriesPropertySynchronizer _imagingStudySeriesPropertySynchronizer;
        private readonly string studyInstanceUid = "111";
        private readonly string seriesInstanceUid = "222";
        private readonly string sopInstanceUid = "333";
        private readonly string patientResourceId = "555";
        private const int NumberOfSeriesRelatedInstances = 1;

        public ImagingStudySeriesPropertySynchronizerTests()
        {
            _imagingStudySeriesPropertySynchronizer = new ImagingStudySeriesPropertySynchronizer();
        }

        [Fact]
        public void GivenATransactionContexAndImagingStudy_WhenprocessedForSeries_ThenDicomPropertiesAreCorrectlyMappedtoSeriesWithinImagingStudy()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            _imagingStudySeriesPropertySynchronizer.Synchronize(context, series);

            Assert.Equal("Series Description", series.Description);
            Assert.Equal("MODALITY", series.Modality.Code);
            Assert.Equal(1, series.Number);
            Assert.Equal(new FhirDateTime(1974, 8, 10, 8, 10, 24, TimeSpan.Zero), series.StartedElement);
        }

        [Fact]
        public void GivenATransactionContextWithUpdatedSeriesDescription_WhenprocessedForSeries_ThenDicomPropertyValuesAreUpdatedAreCorrectly()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            _imagingStudySeriesPropertySynchronizer.Synchronize(context, series);

            Assert.Equal("Series Description", series.Description);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(seriesDescrition: "New Series Description"));

            _imagingStudySeriesPropertySynchronizer.Synchronize(newContext, series);
            Assert.Equal("New Series Description", series.Description);
        }

        [Fact]
        public void GivenATransactionContextWithUpdatedSeriesModality_WhenprocessedForSeries_ThenDicomPropertyValuesAreUpdatedAreCorrectly()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            _imagingStudySeriesPropertySynchronizer.Synchronize(context, series);

            Assert.Equal("MODALITY", series.Modality.Code);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(modalityInSeries: "NEWMODALITY"));

            _imagingStudySeriesPropertySynchronizer.Synchronize(newContext, series);
            Assert.Equal("NEWMODALITY", series.Modality.Code);
        }

        [Fact]
        public void GivenATransactionContextWithUpdatedSeriesNumber_WhenprocessedForSeries_ThenDicomPropertyValuesAreUpdatedAreCorrectly()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            _imagingStudySeriesPropertySynchronizer.Synchronize(context, series);

            Assert.Equal(1, series.Number);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(seriesNumber: "2"));

            _imagingStudySeriesPropertySynchronizer.Synchronize(newContext, series);
            Assert.Equal(2, series.Number);
        }

        [Fact]
        public void GivenATransactionContextWithNoPropertyValueChange_WhenprocessedForSeries_ThenDicomPropertyValuesUpdateIsSkipped()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext();
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            _imagingStudySeriesPropertySynchronizer.Synchronize(context, series);

            Assert.Equal(1, series.Number);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext();

            _imagingStudySeriesPropertySynchronizer.Synchronize(newContext, series);
            Assert.Equal(1, series.Number);
        }

        [Fact]
        public void GivenATransactionContextWithNumberOfInstancesInSeries_WhenprocessedForSeries_ThenDicomPropertyValuesAreUpdatedAreCorrectly()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(numberOfSeriesRelatedInstances: NumberOfSeriesRelatedInstances));
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            _imagingStudySeriesPropertySynchronizer.Synchronize(context, series);

            Assert.Equal(NumberOfSeriesRelatedInstances, series.NumberOfInstances);
        }
    }
}
