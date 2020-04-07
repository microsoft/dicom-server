// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using Dicom.Serialization;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class DicomRetrieveMetadataTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly DicomWebClient _client;

        public DicomRetrieveMetadataTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
        }

        [Theory]
        [InlineData("application/data")]
        [InlineData("application/json")]
        public async Task GivenAnIncorrectAcceptHeader_WhenRetrievingResource_NotAcceptableIsReturned(string acceptHeader)
        {
            // Study
            await RetrieveTransactionResourceTests.ValidateNotAcceptableResponseAsync(
                _client,
                string.Format(DicomWebClient.BaseRetrieveStudyMetadataUriFormat, Guid.NewGuid().ToString()),
                acceptHeader);

            // Series
            await RetrieveTransactionResourceTests.ValidateNotAcceptableResponseAsync(
                _client,
                string.Format(DicomWebClient.BaseRetrieveSeriesMetadataUriFormat, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                acceptHeader);

            // Instance
            await RetrieveTransactionResourceTests.ValidateNotAcceptableResponseAsync(
                _client,
                string.Format(DicomWebClient.BaseRetrieveInstanceMetadataUriFormat, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                acceptHeader);
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenStudyInstanceUidDoesnotExists_ReturnsNotFound()
        {
            string fakeStudyInstanceUid = "1.2.345.6.7";
            HttpResult<IReadOnlyList<DicomDataset>> response = await _client.RetrieveStudyMetadataAsync(fakeStudyInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenStudyandSeriesUidDoesnotExists_ReturnsNotFound()
        {
            string fakeStudyInstanceUid = "1.2.345.6.7";
            string fakeSeriesInstanceUid = "1.2.345.6.8";
            HttpResult<IReadOnlyList<DicomDataset>> response = await _client.RetrieveSeriesMetadataAsync(fakeStudyInstanceUid, fakeSeriesInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenSeriesUidDoesnotExists_ReturnsNotFound()
        {
            DicomDataset storedInstance = await PostDicomFileAsync();
            var dicomInstance = storedInstance.ToDicomInstanceIdentifier();

            string fakeSeriesInstanceUid = "1.2.345.6.7";
            HttpResult<IReadOnlyList<DicomDataset>> response = await _client.RetrieveSeriesMetadataAsync(dicomInstance.StudyInstanceUid, fakeSeriesInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenStudyUidDoesnotExists_ReturnsNotFound()
        {
            DicomDataset storedInstance = await PostDicomFileAsync();
            var dicomInstance = storedInstance.ToDicomInstanceIdentifier();

            string fakeStudyInstanceUid = "1.2.345.6.7";
            HttpResult<IReadOnlyList<DicomDataset>> response = await _client.RetrieveSeriesMetadataAsync(fakeStudyInstanceUid, dicomInstance.SeriesInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenStudySeriesAndInstanceUidDoesnotExists_ReturnsNotFound()
        {
            string fakeStudyInstanceUid = "1.2.345.6.7";
            string fakeSeriesInstanceUid = "1.2.345.6.8";
            string fakeSopInstanceUid = "1.2.345.6.9";

            HttpResult<IReadOnlyList<DicomDataset>> response = await _client.RetrieveInstanceMetadataAsync(fakeStudyInstanceUid, fakeSeriesInstanceUid, fakeSopInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenSopInstanceUidDoesnotExists_ReturnsNotFound()
        {
            DicomDataset storedInstance = await PostDicomFileAsync();
            var dicomInstance = storedInstance.ToDicomInstanceIdentifier();

            string fakeSopInstanceUid = "1.2.345.6.7";

            HttpResult<IReadOnlyList<DicomDataset>> response = await _client.RetrieveInstanceMetadataAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, fakeSopInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenSeriesInstanceUidDoesnotExists_ReturnsNotFound()
        {
            DicomDataset storedInstance = await PostDicomFileAsync();
            var dicomInstance = storedInstance.ToDicomInstanceIdentifier();
            string fakeSopInstanceUid = "1.2.345.6.7";

            HttpResult<IReadOnlyList<DicomDataset>> response = await _client.RetrieveInstanceMetadataAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, fakeSopInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenStudyAndSeriesDoesnotExists_ReturnsNotFound()
        {
            string fakeStudyInstanceUid = "1.2.345.6.7";
            string fakeSeriesInstanceUid = "1.2.345.6.8";

            DicomDataset storedInstance = await PostDicomFileAsync();
            var dicomInstance = storedInstance.ToDicomInstanceIdentifier();

            HttpResult<IReadOnlyList<DicomDataset>> response = await _client.RetrieveInstanceMetadataAsync(fakeStudyInstanceUid, fakeSeriesInstanceUid, dicomInstance.SopInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenStoredDicomFile_WhenRetrievingMetadata_MetadataIsRetrievedCorrectly()
        {
            DicomDataset storedInstance = await PostDicomFileAsync(new DicomDataset()
            {
                { DicomTag.SeriesDescription, "A test series" },
                { DicomTag.PixelData, new byte[] { 1, 2, 3 } },
                new DicomSequence(DicomTag.RegistrationSequence, new DicomDataset()
                {
                    { DicomTag.PatientName, "Test^Patient" },
                    { DicomTag.PixelData, new byte[] { 1, 2, 3 } },
                }),
                { DicomTag.StudyDate, DateTime.UtcNow },
                { new DicomTag(0007, 0008), "Private Tag" },
            });
            var dicomInstance = storedInstance.ToDicomInstanceIdentifier();

            HttpResult<IReadOnlyList<DicomDataset>> metadata = await _client.RetrieveStudyMetadataAsync(dicomInstance.StudyInstanceUid);
            Assert.Single(metadata.Value);
            ValidateResponseMetadataDataset(storedInstance, metadata.Value.Single());

            metadata = await _client.RetrieveSeriesMetadataAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid);
            Assert.Single(metadata.Value);
            ValidateResponseMetadataDataset(storedInstance, metadata.Value.Single());

            metadata = await _client.RetrieveInstanceMetadataAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid);
            Assert.Single(metadata.Value);
            ValidateResponseMetadataDataset(storedInstance, metadata.Value.Single());
        }

        private static void ValidateResponseMetadataDataset(DicomDataset storedDataset, DicomDataset retrievedDataset)
        {
            // Trim the stored dataset to the expected items in the repsonse metadata dataset (remove non-supported value representations).
            DicomDataset expectedDataset = storedDataset.Clone();
            expectedDataset.RemoveBulkDataVrs();

            // Compare result datasets by serializing.
            var jsonDicomConverter = new JsonDicomConverter();
            Assert.Equal(
                JsonConvert.SerializeObject(expectedDataset, jsonDicomConverter),
                JsonConvert.SerializeObject(retrievedDataset, jsonDicomConverter));
            Assert.Equal(expectedDataset.Count(), retrievedDataset.Count());
        }

        private async Task<DicomDataset> PostDicomFileAsync(DicomDataset metadataItems = null)
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile();

            if (metadataItems != null)
            {
                dicomFile1.Dataset.AddOrUpdate(metadataItems);
            }

            HttpResult<DicomDataset> response = await _client.PostAsync(new[] { dicomFile1 });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            DicomSequence successSequence = response.Value.GetSequence(DicomTag.ReferencedSOPSequence);
            ValidationHelpers.ValidateSuccessSequence(successSequence, dicomFile1.Dataset);

            return dicomFile1.Dataset;
        }
    }
}
