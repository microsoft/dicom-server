// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dicom;
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
    public class ImagingStudyInstancePropertySynchronizerTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;
        private readonly IImagingStudyInstancePropertySynchronizer _imagingStudyInstancePropertySynchronizer;
        private readonly string studyInstanceUid = "111";
        private readonly string seriesInstanceUid = "222";
        private readonly string sopInstanceUid = "333";
        private readonly string sopClassUid = "4444";
        private readonly string patientResourceId = "555";
        private readonly DicomValidationConfiguration _dicomValidationConfig = new DicomValidationConfiguration();
        private readonly IExceptionStore _exceptionStore = Substitute.For<IExceptionStore>();

        public ImagingStudyInstancePropertySynchronizerTests()
        {
            _imagingStudyInstancePropertySynchronizer = new ImagingStudyInstancePropertySynchronizer(Options.Create(_dicomValidationConfig), _exceptionStore);
        }

        [Fact]
        public async Task GivenATransactionContexAndImagingStudy_WhenprocessedForInstance_ThenDicomPropertiesAreCorrectlyMappedtoInstanceWithinImagingStudyAsync()
        {
            DicomDataset dataset = FhirTransactionContextBuilder.CreateDicomDataset();

            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(dataset);

            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();
            ImagingStudy.InstanceComponent instance = series.Instance.First();

            await _imagingStudyInstancePropertySynchronizer.SynchronizeAsync(context, instance, DefaultCancellationToken);

            Assert.Equal(sopClassUid, instance.SopClass.Code);
            Assert.Equal(1, instance.Number);
        }

        [Fact]
        public async Task GivenATransactionContextWithUpdatedInstanceNumber_WhenprocessedForInstance_ThenDicomPropertyValuesAreUpdatedCorrectlyAsync()
        {
            DicomDataset dataset = FhirTransactionContextBuilder.CreateDicomDataset();

            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(dataset);

            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();
            ImagingStudy.InstanceComponent instance = series.Instance.First();

            await _imagingStudyInstancePropertySynchronizer.SynchronizeAsync(context, instance, DefaultCancellationToken);

            Assert.Equal(1, instance.Number);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(instanceNumber: "2"));

            await _imagingStudyInstancePropertySynchronizer.SynchronizeAsync(newContext, instance, DefaultCancellationToken);
            Assert.Equal(2, instance.Number);
        }

        [Fact]
        public async Task GivenATransactionContextWithNoDicomPropertyValueChange_WhenprocessedForInstancee_ThenDicomPropertyValuesUpdateIsSkippedAsync()
        {
            DicomDataset dataset = FhirTransactionContextBuilder.CreateDicomDataset();

            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(dataset);

            ImagingStudy.SeriesComponent series = imagingStudy.Series.First();
            ImagingStudy.InstanceComponent instance = series.Instance.First();

            await _imagingStudyInstancePropertySynchronizer.SynchronizeAsync(context, instance, DefaultCancellationToken);

            Assert.Equal(1, instance.Number);

            FhirTransactionContext newContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext(dataset);

            await _imagingStudyInstancePropertySynchronizer.SynchronizeAsync(newContext, instance, DefaultCancellationToken);
            Assert.Equal(1, instance.Number);
        }
    }
}
