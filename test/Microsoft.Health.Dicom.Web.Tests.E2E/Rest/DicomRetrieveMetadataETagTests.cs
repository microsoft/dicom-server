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
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class DicomRetrieveMetadataETagTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly DicomWebClient _client;

        public DicomRetrieveMetadataETagTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenIfNoneMatchMatchesETag_ThenNotModifiedResponseIsReturned()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            DicomDataset storedInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());

            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid);

            string eTag = GetEtagFromResponse(response);

            response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid, eTag);
            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
            Assert.Null(response.Value);    // Make sure that the body is null.
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenIfNoneMatchDoesnotMatchETag_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());

            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid);

            string eTag = GetEtagFromResponse(response);
            string ifNoneMatch = null;
            if (!string.IsNullOrEmpty(eTag))
            {
                ifNoneMatch = string.Concat("1", eTag);
            }

            response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid, ifNoneMatch);
            ValidateResponseMetadataDataset(response, firstStoredInstance, secondStoredInstance);
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenIfNoneMatchIsNotPresent_ThenResponseMetadataIsReturnedWithETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());

            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid);
            ValidateResponseMetadataDataset(response, firstStoredInstance, secondStoredInstance);
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenStudyIsUpdatedAndPreviousETagIsUsed_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());
            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid);

            string eTag = GetEtagFromResponse(response);

            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());

            response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid, eTag);
            ValidateResponseMetadataDataset(response, firstStoredInstance, secondStoredInstance);
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenInstanceIsDeletedInStudyAndPreviousETagIsUsed_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string firstSeriesInstanceUid = TestUidGenerator.Generate();
            string firstSopInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Instance, studyInstanceUid, firstSeriesInstanceUid, firstSopInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());

            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid);
            string eTag = GetEtagFromResponse(response);

            await _client.DeleteInstanceAsync(studyInstanceUid, firstSeriesInstanceUid, firstSopInstanceUid);
            response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid, eTag);
            Assert.Single(response.Value);
            ValidateResponseMetadataDataset(secondStoredInstance, response.Value.First());
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenIfNoneMatchMatchesETag_ThenNotModifiedResponseIsReturned()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();

            DicomDataset storedInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());

            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid);

            string eTag = GetEtagFromResponse(response);

            response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid, eTag);
            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
            Assert.Null(response.Value);    // Make sure that the body is null.
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenIfNoneMatchDoesnotMatchETag_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());

            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid);

            string ifNoneMatch = null;
            string eTag = GetEtagFromResponse(response);
            if (!string.IsNullOrEmpty(eTag))
            {
                ifNoneMatch = string.Concat("1", eTag);
            }

            response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid, ifNoneMatch);
            ValidateResponseMetadataDataset(response, firstStoredInstance, secondStoredInstance);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenIfNoneMatchIsNotPresent_ThenResponseMetadataIsReturnedWithETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());

            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid);
            ValidateResponseMetadataDataset(response, firstStoredInstance, secondStoredInstance);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenSeriesIsUpdatedAndPreviousETagIsUsed_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());
            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid);

            string eTag = GetEtagFromResponse(response);

            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());

            response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid, eTag);
            ValidateResponseMetadataDataset(response, firstStoredInstance, secondStoredInstance);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenInstanceIsDeletedInSeriesAndPreviousETagIsUsed_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string firstSopInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Instance, studyInstanceUid, seriesInstanceUid, firstSopInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());

            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid);
            string eTag = GetEtagFromResponse(response);

            await _client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, firstSopInstanceUid);
            response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid, eTag);
            Assert.Single(response.Value);
            ValidateResponseMetadataDataset(secondStoredInstance, response.Value.First());
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequest_WhenIfNoneMatchMatchesETag_ThenNotModifiedResponseIsReturned()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomDataset storedInstance = await PostDicomFileAsync(ResourceType.Instance, studyInstanceUid, seriesInstanceUid, sopInstanceUid, dataSet: GenerateNewDataSet());

            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            string eTag = GetEtagFromResponse(response);

            response = await _client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, eTag);
            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
            Assert.Null(response.Value);    // Make sure that the body is null.
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequest_WhenIfNoneMatchDoesnotMatchETag_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomDataset storedInstance = await PostDicomFileAsync(ResourceType.Instance, studyInstanceUid, seriesInstanceUid, sopInstanceUid, dataSet: GenerateNewDataSet());

            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            string ifNoneMatch = null;
            string eTag = GetEtagFromResponse(response);

            if (!string.IsNullOrEmpty(eTag))
            {
                ifNoneMatch = string.Concat("1", eTag);
            }

            response = await _client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/dicom+json", response.Content.Headers.ContentType.MediaType);
            Assert.Single(response.Value);
            ValidateResponseMetadataDataset(storedInstance, response.Value.First());
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequest_WhenIfNoneMatchIsNotPresent_ThenResponseMetadataIsReturnedWithETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomDataset storedInstance = await PostDicomFileAsync(ResourceType.Instance, studyInstanceUid, seriesInstanceUid, sopInstanceUid, dataSet: GenerateNewDataSet());

            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/dicom+json", response.Content.Headers.ContentType.MediaType);
            Assert.Single(response.Value);
            ValidateResponseMetadataDataset(storedInstance, response.Value.First());
        }

        private string GetEtagFromResponse(DicomWebResponse<IReadOnlyList<DicomDataset>> response)
        {
            string eTag = null;
            if (response.Headers.TryGetValues(HeaderNames.ETag, out IEnumerable<string> eTagValues))
            {
                if (eTagValues.Count() > 0)
                {
                    eTag = eTagValues.FirstOrDefault();
                }
            }

            return eTag;
        }

        private async Task<DicomDataset> PostDicomFileAsync(ResourceType resourceType, string studyInstanceUid, string seriesInstanceUid = null, string sopInstanceUid = null, DicomDataset dataSet = null)
        {
            DicomFile dicomFile = null;

            switch (resourceType)
            {
                case ResourceType.Study:
                    dicomFile = Samples.CreateRandomDicomFile(studyInstanceUid);
                    break;
                case ResourceType.Series:
                    dicomFile = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid);
                    break;
                case ResourceType.Instance:
                    dicomFile = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
                    break;
            }

            if (dataSet != null)
            {
                dicomFile.Dataset.AddOrUpdate(dataSet);
            }

            DicomWebResponse<DicomDataset> response = await _client.StoreAsync(new[] { dicomFile });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return dicomFile.Dataset;
        }

        private static DicomDataset GenerateNewDataSet()
        {
            return new DicomDataset()
            {
                { DicomTag.SeriesDescription, "A Test Series" },
                { DicomTag.PixelData, new byte[] { 1, 2, 3 } },
                new DicomSequence(DicomTag.RegistrationSequence, new DicomDataset()
                {
                    { DicomTag.PatientName, "Test^Patient" },
                    { DicomTag.PixelData, new byte[] { 1, 2, 3 } },
                }),
                { DicomTag.StudyDate, DateTime.UtcNow },
                { new DicomTag(0007, 0008), "Private Tag" },
            };
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

        private void ValidateResponseMetadataDataset(DicomWebResponse<IReadOnlyList<DicomDataset>> response, DicomDataset storedInstance1, DicomDataset storedInstance2)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/dicom+json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal(2, response.Value.Count());

            // Trim the stored dataset to the expected items in the response metadata dataset (remove non-supported value representations).
            DicomDataset expectedDataset1 = storedInstance1.CopyWithoutBulkDataItems();
            DicomDataset expectedDataset2 = storedInstance2.CopyWithoutBulkDataItems();

            DicomDataset retrievedDataset1 = response.Value.First();
            DicomDataset retrievedDataset2 = response.Value.Last();

            // Compare result datasets by serializing.
            var jsonDicomConverter = new JsonDicomConverter();

            string serializedExpectedDataset1 = JsonConvert.SerializeObject(expectedDataset1, jsonDicomConverter);
            string serializedExpectedDataset2 = JsonConvert.SerializeObject(expectedDataset2, jsonDicomConverter);

            string serializedRetrievedDataset1 = JsonConvert.SerializeObject(retrievedDataset1, jsonDicomConverter);
            string serializedRetrievedDataset2 = JsonConvert.SerializeObject(retrievedDataset2, jsonDicomConverter);

            if (string.Equals(serializedExpectedDataset1, serializedRetrievedDataset1, StringComparison.InvariantCultureIgnoreCase) && string.Equals(serializedExpectedDataset2, serializedRetrievedDataset2, StringComparison.InvariantCultureIgnoreCase))
            {
                Assert.Equal(expectedDataset1.Count(), retrievedDataset1.Count());
                Assert.Equal(expectedDataset2.Count(), retrievedDataset2.Count());
                return;
            }
            else if (string.Equals(serializedExpectedDataset2, serializedRetrievedDataset1, StringComparison.InvariantCultureIgnoreCase) && string.Equals(serializedExpectedDataset1, serializedRetrievedDataset2, StringComparison.InvariantCultureIgnoreCase))
            {
                Assert.Equal(expectedDataset2.Count(), retrievedDataset1.Count());
                Assert.Equal(expectedDataset1.Count(), retrievedDataset2.Count());
                return;
            }

            Assert.False(true, "Retrieved dataset doesnot match the stored dataset");
        }
    }
}
