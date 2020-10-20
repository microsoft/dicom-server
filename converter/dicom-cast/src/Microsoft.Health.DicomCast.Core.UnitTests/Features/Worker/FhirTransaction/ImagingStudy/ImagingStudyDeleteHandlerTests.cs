// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class ImagingStudyDeleteHandlerTests
    {
        private const string DefaultDicomWebEndpoint = "https://dicom/";

        private readonly IFhirService _fhirService;
        private readonly ImagingStudyDeleteHandler _imagingStudyDeleteHandler;
        private readonly DicomWebConfiguration _configuration;

        private FhirTransactionContext _fhirTransactionContext;

        public ImagingStudyDeleteHandlerTests()
        {
            _configuration = new DicomWebConfiguration() { Endpoint = new System.Uri(DefaultDicomWebEndpoint), };
            IOptions<DicomWebConfiguration> optionsConfiguration = Options.Create(_configuration);

            _fhirService = Substitute.For<IFhirService>();
            _imagingStudyDeleteHandler = new ImagingStudyDeleteHandler(_fhirService, optionsConfiguration);
        }

        [Fact]
        public async Task GivenAChangeFeedEntryToDeleteAnInstanceWithinASeriesContainingMoreThanOneInstance_WhenBuilt_ThenCorrectEntryComponentShouldBeCreated()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string sopInstanceUid = "3";
            const string sopInstanceUid1 = "3";
            const string patientResourceId = "p1";

            // create a new ImagingStudy
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid, sopInstanceUid1 }, patientResourceId);
            _fhirService.RetrieveImagingStudyAsync(Arg.Any<Identifier>(), Arg.Any<CancellationToken>()).Returns(imagingStudy);

            // delete an existing instance within a study
            FhirTransactionRequestEntry entry = await BuildImagingStudyEntryComponent(studyInstanceUid, seriesInstanceUid, sopInstanceUid, patientResourceId);

            ImagingStudy updatedImagingStudy = ValidationUtility.ValidateImagingStudyUpdate(studyInstanceUid, patientResourceId, entry);

            Assert.Equal(ImagingStudy.ImagingStudyStatus.Available, updatedImagingStudy.Status);

            Assert.Collection(
                updatedImagingStudy.Series,
                series =>
                {
                    Assert.Equal(seriesInstanceUid, series.Uid);

                    Assert.Collection(
                        series.Instance,
                        instance => Assert.Equal(sopInstanceUid1, instance.Uid));
                });
        }

        [Fact]
        public async Task GivenAChangeFeedEntryToDeleteAnInstanceWithinASeriesContainingOneInstanceDifferentSouce_WhenBuilt_ShouldUpdateNotDelete()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string sopInstanceUid = "3";
            const string patientResourceId = "p1";

            // create a new ImagingStudy
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            _fhirService.RetrieveImagingStudyAsync(Arg.Any<Identifier>(), Arg.Any<CancellationToken>()).Returns(imagingStudy);

            // delete an existing instance within a study
            FhirTransactionRequestEntry entry = await BuildImagingStudyEntryComponent(studyInstanceUid, seriesInstanceUid, sopInstanceUid, patientResourceId);

            Assert.Equal(FhirTransactionRequestMode.Update, entry.RequestMode);
        }

        [Fact]
        public async Task GivenAChangeFeedEntryToDeleteAnInstanceWithinASeriesContainingOneInstanceSameSouce_WhenBuilt_ShouldDelete()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string sopInstanceUid = "3";
            const string patientResourceId = "p1";

            // create a new ImagingStudy
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId, DefaultDicomWebEndpoint);
            _fhirService.RetrieveImagingStudyAsync(Arg.Any<Identifier>(), Arg.Any<CancellationToken>()).Returns(imagingStudy);

            // delete an existing instance within a study
            FhirTransactionRequestEntry entry = await BuildImagingStudyEntryComponent(studyInstanceUid, seriesInstanceUid, sopInstanceUid, patientResourceId);

            Assert.Equal(FhirTransactionRequestMode.Delete, entry.RequestMode);
        }

        [Fact]
        public async Task GivenAChangeFeedEntryToDeleteAnInstanceWithinAStudyContainingMoreThanOneSeries_WhenBuilt_ThenCorrectEntryComponentShouldBeCreated()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string seriesInstanceUid1 = "3";
            const string sopInstanceUid = "3";
            const string patientResourceId = "p1";

            // create a new ImagingStudy
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid, seriesInstanceUid1 }, new List<string>() { sopInstanceUid, }, patientResourceId);
            _fhirService.RetrieveImagingStudyAsync(Arg.Any<Identifier>(), Arg.Any<CancellationToken>()).Returns(imagingStudy);

            // delete an existing instance within a study
            FhirTransactionRequestEntry entry = await BuildImagingStudyEntryComponent(studyInstanceUid, seriesInstanceUid, sopInstanceUid, patientResourceId);

            ImagingStudy updatedImagingStudy = ValidationUtility.ValidateImagingStudyUpdate(studyInstanceUid, patientResourceId, entry);

            Assert.Equal(ImagingStudy.ImagingStudyStatus.Available, updatedImagingStudy.Status);

            Assert.Collection(
                updatedImagingStudy.Series,
                series =>
                {
                    ValidationUtility.ValidateSeries(series, seriesInstanceUid1, sopInstanceUid);
                });
        }

        [Fact]
        public async Task GivenAChangeFeedEntryForDeleteInstanceThatDoesNotExistsWithinGivenStudy_WhenBuilt_ThenNoEntryComponentShouldBeCreated()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string sopInstanceUid = "3";
            const string sopInstanceUid1 = "4";
            const string patientResourceId = "p1";

            // create a new ImagingStudy
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(studyInstanceUid, new List<string>() { seriesInstanceUid }, new List<string>() { sopInstanceUid }, patientResourceId);
            _fhirService.RetrieveImagingStudyAsync(Arg.Any<Identifier>(), Arg.Any<CancellationToken>()).Returns(imagingStudy);

            // try delete non-existing instance within a study
            FhirTransactionRequestEntry entry = await BuildImagingStudyEntryComponent(studyInstanceUid, seriesInstanceUid, sopInstanceUid1, patientResourceId);

            Assert.Null(entry);
        }

        [Fact]
        public async Task GivenAChangeFeedEntryForDeleteForStudyInstanceThatDoesNotExists_WhenBuilt_ThenNoEntryComponentShouldBeCreated()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string sopInstanceUid = "3";

            // try delete instance from a non-existing study
            FhirTransactionRequestEntry entry = await BuildImagingStudyEntryComponent(studyInstanceUid, seriesInstanceUid, sopInstanceUid, patientResourceId: "p1");

            Assert.Null(entry);
        }

        private async Task<FhirTransactionRequestEntry> BuildImagingStudyEntryComponent(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string patientResourceId)
        {
            ChangeFeedEntry changeFeedEntry = ChangeFeedGenerator.Generate(action: ChangeFeedAction.Delete, studyInstanceUid: studyInstanceUid, seriesInstanceUid: seriesInstanceUid, sopInstanceUid: sopInstanceUid);
            return await PrepareRequestAsync(changeFeedEntry, patientResourceId);
        }

        private async Task<FhirTransactionRequestEntry> PrepareRequestAsync(ChangeFeedEntry changeFeedEntry, string patientResourceId)
        {
            _fhirTransactionContext = new FhirTransactionContext(changeFeedEntry);

            _fhirTransactionContext.Request.Patient = FhirTransactionRequestEntryGenerator.GenerateDefaultNoChangeRequestEntry<Patient>(new ServerResourceId(ResourceType.Patient, patientResourceId));

            return await _imagingStudyDeleteHandler.BuildAsync(_fhirTransactionContext, CancellationToken.None);
        }
    }
}
