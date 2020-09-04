// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class ImagingStudyUpsertHandlerTests
    {
        private readonly IFhirService _fhirService;
        private readonly IImagingStudySynchronizer _imagingStudySynchronizer;
        private readonly ImagingStudyUpsertHandler _imagingStudyUpsertHandler;

        private FhirTransactionContext _fhirTransactionContext;

        public ImagingStudyUpsertHandlerTests()
        {
            _fhirService = Substitute.For<IFhirService>();
            _imagingStudySynchronizer = new ImagingStudySynchronizer(new ImagingStudyPropertySynchronizer(), new ImagingStudySeriesPropertySynchronizer(), new ImagingStudyInstancePropertySynchronizer());
            _imagingStudyUpsertHandler = new ImagingStudyUpsertHandler(_fhirService, _imagingStudySynchronizer);
        }

        [Fact]
        public async Task GivenAValidCreateChangeFeed_WhenBuilt_ThenCorrectEntryComponentShouldBeCreated()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string sopInstanceUid = "3";
            const string patientResourceId = "p1";

            ChangeFeedEntry changeFeedEntry = ChangeFeedGenerator.Generate(
                action: ChangeFeedAction.Create,
                studyInstanceUid: studyInstanceUid,
                seriesInstanceUid: seriesInstanceUid,
                sopInstanceUid: sopInstanceUid);

            FhirTransactionRequestEntry entry = await BuildImagingStudyEntryComponent(studyInstanceUid, seriesInstanceUid, sopInstanceUid, patientResourceId);

            ValidationUtility.ValidateRequestEntryMinimumRequirementForWithChange(FhirTransactionRequestMode.Create, "ImagingStudy", Bundle.HTTPVerb.POST, entry);

            ImagingStudy imagingStudy = Assert.IsType<ImagingStudy>(entry.Resource);

            string jsonString;
            jsonString = JsonSerializer.Serialize(entry);

            Assert.IsType<ClientResourceId>(entry.ResourceId);
            Assert.Equal(ImagingStudy.ImagingStudyStatus.Available, imagingStudy.Status);
            Assert.Null(entry.Request.IfMatch);

            ValidationUtility.ValidateResourceReference("Patient/p1", imagingStudy.Subject);

            Assert.Collection(
                imagingStudy.Identifier,
                identifier => ValidationUtility.ValidateIdentifier("urn:dicom:uid", $"urn:oid:{studyInstanceUid}", identifier));

            Assert.Collection(
                imagingStudy.Series,
                series =>
                {
                    Assert.Equal(seriesInstanceUid, series.Uid);

                    Assert.Collection(
                        series.Instance,
                        instance => Assert.Equal(sopInstanceUid, instance.Uid));
                });

            ValidateDicomFilePropertiesAreCorrectlyMapped(imagingStudy, series: imagingStudy.Series.First(), instance: imagingStudy.Series.First().Instance.First());
        }

        [Fact]
        public async Task GivenAChangeFeedWithNewSeriesAndInstanceForAnExistingImagingStudy_WhenBuilt_ThenCorrectEntryComponentShouldBeCreated()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string sopInstanceUid = "3";
            const string patientResourceId = "p1";
            const string newSeriesInstanceUid = "3";
            const string newSopInstanceUid = "4";

            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);

            _fhirService.RetrieveImagingStudyAsync(Arg.Any<Identifier>(), Arg.Any<CancellationToken>()).Returns(imagingStudy);

            // Update an existing ImagingStudy
            FhirTransactionRequestEntry entry = await BuildImagingStudyEntryComponent(studyInstanceUid, newSeriesInstanceUid, newSopInstanceUid, patientResourceId);

            ImagingStudy updatedImagingStudy = ValidationUtility.ValidateImagingStudyUpdate(studyInstanceUid, patientResourceId, entry);

            Assert.Collection(
                updatedImagingStudy.Series,
                series =>
                {
                    ValidationUtility.ValidateSeries(series, seriesInstanceUid, sopInstanceUid);
                },
                series =>
                {
                    ValidationUtility.ValidateSeries(series, newSeriesInstanceUid, newSopInstanceUid);
                });
        }

        [Fact]
        public async Task GivenAChangeFeedWithNewInstanceForAnExistingSeriesAndImagingStudy_WhenBuilt_ThenCorrectEntryComponentShouldBeCreated()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string sopInstanceUid = "3";
            const string patientResourceId = "p1";
            const string newSopInstanceUid = "4";

            // create a new ImagingStudy
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);

            _fhirService.RetrieveImagingStudyAsync(Arg.Any<Identifier>(), Arg.Any<CancellationToken>()).Returns(imagingStudy);

            // update an existing ImagingStudy
            FhirTransactionRequestEntry entry = await BuildImagingStudyEntryComponent(studyInstanceUid, seriesInstanceUid, newSopInstanceUid, patientResourceId);

            ImagingStudy updatedImagingStudy = ValidationUtility.ValidateImagingStudyUpdate(studyInstanceUid, patientResourceId, entry);

            Assert.Collection(
                updatedImagingStudy.Series,
                series =>
                {
                    ValidationUtility.ValidateSeries(series, seriesInstanceUid, sopInstanceUid, newSopInstanceUid);
                });
        }

        [Fact]
        public async Task GivenAChangeFeedWithExistingInstanceForAnExistingSeriesAndImagingStudy_WhenBuilt_ThenNoEntryComponentShouldBeReturned()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string sopInstanceUid = "3";
            const string patientResourceId = "p1";

            // create a new ImagingStudy
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, $"Patient/{patientResourceId}");

            _fhirService.RetrieveImagingStudyAsync(Arg.Any<Identifier>(), Arg.Any<CancellationToken>()).Returns(imagingStudy);

            // try update an existing ImagingStudy
            FhirTransactionRequestEntry entry = await BuildImagingStudyEntryComponent(studyInstanceUid, seriesInstanceUid, sopInstanceUid, patientResourceId, addMetadata: false);

            // Validate no entry component is created as there is no update
            Assert.NotNull(entry);
            Assert.Equal(FhirTransactionRequestMode.None, entry.RequestMode);
            Assert.Null(entry.Request);
            Assert.IsType<ServerResourceId>(entry.ResourceId);
            Assert.True(imagingStudy.IsExactly(entry.Resource));
        }

        [Fact]
        public async Task GivenAChangeFeedWithNewInstanceAndNewSeiresForAnExistingImagingStudy_WhenBuilt_ThenCorrectEtagIsGenerated()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string sopInstanceUid = "3";
            const string newSeriesInstanceUid = "3";
            const string newSopInstanceUid = "3";
            const string patientResourceId = "p1";

            // create a new ImagingStudy
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);

            _fhirService.RetrieveImagingStudyAsync(Arg.Any<Identifier>(), Arg.Any<CancellationToken>()).Returns(imagingStudy);

            // update an existing ImagingStudy
            FhirTransactionRequestEntry entry = await BuildImagingStudyEntryComponent(studyInstanceUid, newSeriesInstanceUid, newSopInstanceUid, patientResourceId);

            string expectedIfMatchCondition = $"W/\"1\"";

            Assert.Equal(expectedIfMatchCondition, entry.Request.IfMatch);
        }

        [Fact]
        public async Task GivenAChangeFeedWithNewInstanceAndNewSeiresForAnExistingImagingStudy_WhenBuilt_ThenCorrectEtagIsGeneratedd()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string sopInstanceUid = "3";
            const string newSeriesInstanceUid = "3";
            const string newSopInstanceUid = "3";
            const string patientResourceId = "p1";

            // create a new ImagingStudy
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);

            _fhirService.RetrieveImagingStudyAsync(Arg.Any<Identifier>(), Arg.Any<CancellationToken>()).Returns(imagingStudy);

            // update an existing ImagingStudy
            FhirTransactionRequestEntry entry = await BuildImagingStudyEntryComponent(studyInstanceUid, newSeriesInstanceUid, newSopInstanceUid, patientResourceId);

            string expectedIfMatchCondition = $"W/\"1\"";

            Assert.Equal(expectedIfMatchCondition, entry.Request.IfMatch);
        }

        private async Task<FhirTransactionRequestEntry> BuildImagingStudyEntryComponent(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string patientResourceId, bool addMetadata = true)
        {
            ChangeFeedEntry changeFeedEntry = ChangeFeedGenerator.Generate(
                action: ChangeFeedAction.Create,
                studyInstanceUid: studyInstanceUid,
                seriesInstanceUid: seriesInstanceUid,
                sopInstanceUid: sopInstanceUid,
                metadata: addMetadata ? FhirTransactionContextBuilder.CreateDicomDataset() : null);

            return await PrepareRequestAsync(changeFeedEntry, patientResourceId);
        }

        private async Task<FhirTransactionRequestEntry> PrepareRequestAsync(ChangeFeedEntry changeFeedEntry, string patientResourceId)
        {
            _fhirTransactionContext = new FhirTransactionContext(changeFeedEntry);

            _fhirTransactionContext.Request.Patient = FhirTransactionRequestEntryGenerator.GenerateDefaultNoChangeRequestEntry<Patient>(
                new ServerResourceId(ResourceType.Patient, patientResourceId));

            _fhirTransactionContext.Request.Endpoint = FhirTransactionRequestEntryGenerator.GenerateDefaultNoChangeRequestEntry<Endpoint>(
                new ServerResourceId(ResourceType.Endpoint, "endpoint"));

            return await _imagingStudyUpsertHandler.BuildAsync(_fhirTransactionContext, CancellationToken.None);
        }

        private void ValidateDicomFilePropertiesAreCorrectlyMapped(ImagingStudy imagingStudy, ImagingStudy.SeriesComponent series, ImagingStudy.InstanceComponent instance)
        {
            Assert.Collection(
               imagingStudy.Endpoint,
               reference => string.Equals(reference.Reference, _fhirTransactionContext.Request.Endpoint.ToString(), StringComparison.Ordinal));

            // Assert imaging study properties are mapped correctly
            Assert.Collection(
               imagingStudy.Modality,
               modality => string.Equals(modality.Code, "MODALITY", StringComparison.Ordinal));

            Assert.Collection(
               imagingStudy.Note,
               note => string.Equals(note.Text.ToString(), "Study Description", StringComparison.Ordinal));

            Assert.Equal(new FhirDateTime(1974, 7, 10, 7, 10, 24, TimeSpan.Zero), imagingStudy.StartedElement);

            // Assert series properties are mapped correctly
            Assert.Equal("Series Description", series.Description);
            Assert.Equal("MODALITY", series.Modality.Code);
            Assert.Equal(1, series.Number);
            Assert.Equal(new FhirDateTime(1974, 8, 10, 8, 10, 24, TimeSpan.Zero), series.StartedElement);

            // Assert instance properties are mapped correctly
            Assert.Equal("4444", instance.SopClass.Code);
            Assert.Equal(1, instance.Number);
        }
    }
}
