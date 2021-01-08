// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class ImagingStudySeriesPropertySynchronizerTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;
        private readonly IImagingStudySeriesPropertySynchronizer _imagingStudySeriesPropertySynchronizer;
        private readonly string studyInstanceUid = "111";
        private readonly string seriesInstanceUid = "222";
        private readonly string sopInstanceUid = "333";
        private readonly string patientResourceId = "555";

        private readonly DicomValidationConfiguration _dicomValidationConfig = new DicomValidationConfiguration();
        private readonly IExceptionStore _exceptionStore = Substitute.For<IExceptionStore>();

        public ImagingStudySeriesPropertySynchronizerTests()
        {
            _imagingStudySeriesPropertySynchronizer = new ImagingStudySeriesPropertySynchronizer(_dicomValidationConfig, _exceptionStore);
        }

        [Fact]
        public void GivenATransactionContexAndImagingStudy_WhenprocessedForSeries_ThenDicomPropertiesAreCorrectlyMappedtoSeriesWithinImagingStudy()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            _imagingStudySeriesPropertySynchronizer.Synchronize(context, series, DefaultCancellationToken);

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

            _imagingStudySeriesPropertySynchronizer.Synchronize(context, series, DefaultCancellationToken);

            Assert.Equal("Series Description", series.Description);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(seriesDescrition: "New Series Description"));

            _imagingStudySeriesPropertySynchronizer.Synchronize(newContext, series, DefaultCancellationToken);
            Assert.Equal("New Series Description", series.Description);
        }

        [Fact]
        public void GivenATransactionContextWithUpdatedSeriesModality_WhenprocessedForSeries_ThenDicomPropertyValuesAreUpdatedAreCorrectly()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            _imagingStudySeriesPropertySynchronizer.Synchronize(context, series, DefaultCancellationToken);

            Assert.Equal("MODALITY", series.Modality.Code);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(modalityInSeries: "NEWMODALITY"));

            _imagingStudySeriesPropertySynchronizer.Synchronize(newContext, series, DefaultCancellationToken);
            Assert.Equal("NEWMODALITY", series.Modality.Code);
        }

        [Fact]
        public void GivenATransactionContextWithUpdatedSeriesNumber_WhenprocessedForSeries_ThenDicomPropertyValuesAreUpdatedAreCorrectly()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            _imagingStudySeriesPropertySynchronizer.Synchronize(context, series, DefaultCancellationToken);

            Assert.Equal(1, series.Number);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(seriesNumber: "2"));

            _imagingStudySeriesPropertySynchronizer.Synchronize(newContext, series, DefaultCancellationToken);
            Assert.Equal(2, series.Number);
        }

        [Fact]
        public void GivenATransactionContextWithNoPropertyValueChange_WhenprocessedForSeries_ThenDicomPropertyValuesUpdateIsSkipped()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext();
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            _imagingStudySeriesPropertySynchronizer.Synchronize(context, series, DefaultCancellationToken);

            Assert.Equal(1, series.Number);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext();

            _imagingStudySeriesPropertySynchronizer.Synchronize(newContext, series, DefaultCancellationToken);
            Assert.Equal(1, series.Number);
        }
    }
}
