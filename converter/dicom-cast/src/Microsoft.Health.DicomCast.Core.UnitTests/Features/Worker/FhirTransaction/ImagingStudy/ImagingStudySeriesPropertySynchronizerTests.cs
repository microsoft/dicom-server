// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class ImagingStudySeriesPropertySynchronizerTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;
        private readonly IImagingStudySeriesPropertySynchronizer _imagingStudySeriesPropertySynchronizer;
        private const string StudyInstanceUid = "111";
        private const string SeriesInstanceUid = "222";
        private const string SopInstanceUid = "333";
        private const string PatientResourceId = "555";

        private readonly DicomCastConfiguration _dicomCastConfig = new DicomCastConfiguration();
        private readonly IExceptionStore _exceptionStore = Substitute.For<IExceptionStore>();

        public ImagingStudySeriesPropertySynchronizerTests()
        {
            _imagingStudySeriesPropertySynchronizer = new ImagingStudySeriesPropertySynchronizer(Options.Create(_dicomCastConfig), _exceptionStore);
        }

        [Fact]
        public async Task GivenATransactionContexAndImagingStudy_WhenprocessedForSeries_ThenDicomPropertiesAreCorrectlyMappedtoSeriesWithinImagingStudyAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(StudyInstanceUid, new List<string>() { SeriesInstanceUid }, new List<string>() { SopInstanceUid }, PatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            await _imagingStudySeriesPropertySynchronizer.SynchronizeAsync(context, series, DefaultCancellationToken);

            Assert.Equal("Series Description", series.Description);
            Assert.Equal("MODALITY", series.Modality.Code);
            Assert.Equal(1, series.Number);
            Assert.Equal(new FhirDateTime(1974, 8, 10, 8, 10, 24, TimeSpan.Zero), series.StartedElement);
        }

        [Fact]
        public async Task GivenATransactionContextWithUpdatedSeriesDescription_WhenprocessedForSeries_ThenDicomPropertyValuesAreUpdatedAreCorrectlyAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(StudyInstanceUid, new List<string>() { SeriesInstanceUid }, new List<string>() { SopInstanceUid }, PatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            await _imagingStudySeriesPropertySynchronizer.SynchronizeAsync(context, series, DefaultCancellationToken);

            Assert.Equal("Series Description", series.Description);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(seriesDescrition: "New Series Description"));

            await _imagingStudySeriesPropertySynchronizer.SynchronizeAsync(newContext, series, DefaultCancellationToken);
            Assert.Equal("New Series Description", series.Description);
        }

        [Fact]
        public async Task GivenATransactionContextWithUpdatedSeriesModality_WhenprocessedForSeries_ThenDicomPropertyValuesAreUpdatedAreCorrectlyAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(StudyInstanceUid, new List<string>() { SeriesInstanceUid }, new List<string>() { SopInstanceUid }, PatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            await _imagingStudySeriesPropertySynchronizer.SynchronizeAsync(context, series, DefaultCancellationToken);

            Assert.Equal("MODALITY", series.Modality.Code);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(modalityInSeries: "NEWMODALITY"));

            await _imagingStudySeriesPropertySynchronizer.SynchronizeAsync(newContext, series, DefaultCancellationToken);
            Assert.Equal("NEWMODALITY", series.Modality.Code);
        }

        [Fact]
        public async Task GivenATransactionContextWithUpdatedSeriesNumber_WhenprocessedForSeries_ThenDicomPropertyValuesAreUpdatedAreCorrectlyAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(StudyInstanceUid, new List<string>() { SeriesInstanceUid }, new List<string>() { SopInstanceUid }, PatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            await _imagingStudySeriesPropertySynchronizer.SynchronizeAsync(context, series, DefaultCancellationToken);

            Assert.Equal(1, series.Number);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(seriesNumber: "2"));

            await _imagingStudySeriesPropertySynchronizer.SynchronizeAsync(newContext, series, DefaultCancellationToken);
            Assert.Equal(2, series.Number);
        }

        [Fact]
        public async Task GivenATransactionContextWithNoPropertyValueChange_WhenprocessedForSeries_ThenDicomPropertyValuesUpdateIsSkippedAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(StudyInstanceUid, new List<string>() { SeriesInstanceUid }, new List<string>() { SopInstanceUid }, PatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext();
            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();

            await _imagingStudySeriesPropertySynchronizer.SynchronizeAsync(context, series, DefaultCancellationToken);

            Assert.Equal(1, series.Number);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext();

            await _imagingStudySeriesPropertySynchronizer.SynchronizeAsync(newContext, series, DefaultCancellationToken);
            Assert.Equal(1, series.Number);
        }
    }
}
