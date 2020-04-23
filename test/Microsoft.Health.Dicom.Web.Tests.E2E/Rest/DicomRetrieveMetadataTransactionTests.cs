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
        public async Task GivenAnIncorrectAcceptHeader_WhenRetrievingResource_ThenNotAcceptableIsReturned(string acceptHeader)
        {
            // Study
            await RetrieveTransactionResourceTests.ValidateNotAcceptableResponseAsync(
                _client,
                string.Format(DicomWebConstants.BaseRetrieveStudyMetadataUriFormat, Guid.NewGuid().ToString()),
                acceptHeader);

            // Series
            await RetrieveTransactionResourceTests.ValidateNotAcceptableResponseAsync(
                _client,
                string.Format(DicomWebConstants.BaseRetrieveSeriesMetadataUriFormat, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                acceptHeader);

            // Instance
            await RetrieveTransactionResourceTests.ValidateNotAcceptableResponseAsync(
                _client,
                string.Format(DicomWebConstants.BaseRetrieveInstanceMetadataUriFormat, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                acceptHeader);
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenStudyInstanceUidDoesnotExists_ThenNotFoundIsReturned()
        {
            string fakeStudyInstanceUid = "1.2.345.6.7";

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveStudyMetadataAsync(fakeStudyInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenStudyandSeriesInstanceUidDoesnotExists_ThenNotFoundIsReturned()
        {
            string fakeStudyInstanceUid = "1.2.345.6.7";
            string fakeSeriesInstanceUid = "1.2.345.6.8";

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveSeriesMetadataAsync(fakeStudyInstanceUid, fakeSeriesInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenSeriesInstanceUidDoesnotExists_ThenNotFoundIsReturned()
        {
            DicomDataset storedInstance = await PostDicomFileAsync();
            var dicomInstance = storedInstance.ToDicomInstanceIdentifier();

            string fakeSeriesInstanceUid = "1.2.345.6.7";

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveSeriesMetadataAsync(dicomInstance.StudyInstanceUid, fakeSeriesInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenStudyInstanceUidDoesnotExists_ThenNotFoundIsReturned()
        {
            DicomDataset storedInstance = await PostDicomFileAsync();
            var dicomInstance = storedInstance.ToDicomInstanceIdentifier();

            string fakeStudyInstanceUid = "1.2.345.6.7";

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveSeriesMetadataAsync(fakeStudyInstanceUid, dicomInstance.SeriesInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenStudySeriesAndSopInstanceUidDoesnotExists_ThenNotFoundIsReturned()
        {
            string fakeStudyInstanceUid = "1.2.345.6.7";
            string fakeSeriesInstanceUid = "1.2.345.6.8";
            string fakeSopInstanceUid = "1.2.345.6.9";

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceMetadataAsync(fakeStudyInstanceUid, fakeSeriesInstanceUid, fakeSopInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenSopInstanceUidDoesnotExists_ThenNotFoundIsReturned()
        {
            DicomDataset storedInstance = await PostDicomFileAsync();
            var dicomInstance = storedInstance.ToDicomInstanceIdentifier();

            string fakeSopInstanceUid = "1.2.345.6.7";

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceMetadataAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, fakeSopInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenSeriesInstanceUidDoesnotExists_ThenNotFoundIsReturned()
        {
            DicomDataset storedInstance = await PostDicomFileAsync();
            var dicomInstance = storedInstance.ToDicomInstanceIdentifier();
            string fakeSopInstanceUid = "1.2.345.6.7";

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceMetadataAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, fakeSopInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenStudyAndSeriesDoesnotExists_ThenNotFoundIsReturned()
        {
            string fakeStudyInstanceUid = "1.2.345.6.7";
            string fakeSeriesInstanceUid = "1.2.345.6.8";

            DicomDataset storedInstance = await PostDicomFileAsync();
            var dicomInstance = storedInstance.ToDicomInstanceIdentifier();

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceMetadataAsync(fakeStudyInstanceUid, fakeSeriesInstanceUid, dicomInstance.SopInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenStoredDicomFile_WhenRetrievingMetadata_ThenMetadataIsRetrievedCorrectly()
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

            DicomWebResponse<IReadOnlyList<DicomDataset>> metadata = await _client.RetrieveStudyMetadataAsync(dicomInstance.StudyInstanceUid);
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
            // Trim the stored dataset to the expected items in the response metadata dataset (remove non-supported value representations).
            DicomDataset expectedDataset = storedDataset.CopyWithoutBulkDataItems();

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

            await _client.StoreAsync(new[] { dicomFile1 });

            return dicomFile1.Dataset;
        }
    }
}
