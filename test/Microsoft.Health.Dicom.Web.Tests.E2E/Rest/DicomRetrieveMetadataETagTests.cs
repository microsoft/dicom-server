// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FellowOakDicom;
using EnsureThat;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Net.Http.Headers;
using Xunit;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using System.Text.Json;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class DicomRetrieveMetadataETagTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly IDicomWebClient _client;

        public DicomRetrieveMetadataETagTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _client = fixture.Client;
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenIfNoneMatchMatchesETag_ThenNotModifiedResponseIsReturned()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            DicomDataset storedInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());
            string eTag;

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid))
            {
                eTag = GetEtagFromResponse(response);
            }

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid, eTag))
            {
                Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
                ValidateNoContent(response);
            }
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenIfNoneMatchDoesnotMatchETag_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());
            string ifNoneMatch = null;

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid))
            {
                string eTag = GetEtagFromResponse(response);
                if (!string.IsNullOrEmpty(eTag))
                {
                    ifNoneMatch = string.Concat("1", eTag);
                }
            }

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid, ifNoneMatch))
            {
                await ValidateResponseMetadataDatasetAsync(response, firstStoredInstance, secondStoredInstance);
            }
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenIfNoneMatchIsNotPresent_ThenResponseMetadataIsReturnedWithETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid);
            await ValidateResponseMetadataDatasetAsync(response, firstStoredInstance, secondStoredInstance);
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenStudyIsUpdatedAndPreviousETagIsUsed_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());
            string eTag;

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid))
            {
                eTag = GetEtagFromResponse(response);
            }

            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid, eTag))
            {
                await ValidateResponseMetadataDatasetAsync(response, firstStoredInstance, secondStoredInstance);
            }
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenInstanceIsDeletedInStudyAndPreviousETagIsUsed_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string firstSeriesInstanceUid = TestUidGenerator.Generate();
            string firstSopInstanceUid = TestUidGenerator.Generate();

            await PostDicomFileAsync(ResourceType.Instance, studyInstanceUid, firstSeriesInstanceUid, firstSopInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());
            string eTag;

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid))
            {
                eTag = GetEtagFromResponse(response);
            }

            await _client.DeleteInstanceAsync(studyInstanceUid, firstSeriesInstanceUid, firstSopInstanceUid);

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid, eTag))
            {
                DicomDataset[] datasets = await response.ToArrayAsync();

                Assert.Single(datasets);
                ValidateResponseMetadataDataset(secondStoredInstance, datasets[0]);
            }
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenIfNoneMatchMatchesETag_ThenNotModifiedResponseIsReturned()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();

            await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());
            string eTag;

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid))
            {
                eTag = GetEtagFromResponse(response);
            }

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid, eTag))
            {
                Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
                ValidateNoContent(response);
            }
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenIfNoneMatchDoesnotMatchETag_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());
            string ifNoneMatch;
            string eTag;

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid))
            {
                ifNoneMatch = null;
                eTag = GetEtagFromResponse(response);
                if (!string.IsNullOrEmpty(eTag))
                {
                    ifNoneMatch = string.Concat("1", eTag);
                }
            }

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid, ifNoneMatch))
            {
                await ValidateResponseMetadataDatasetAsync(response, firstStoredInstance, secondStoredInstance);
            }
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenIfNoneMatchIsNotPresent_ThenResponseMetadataIsReturnedWithETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid);
            await ValidateResponseMetadataDatasetAsync(response, firstStoredInstance, secondStoredInstance);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenSeriesIsUpdatedAndPreviousETagIsUsed_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());
            string eTag;
            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid))
            {
                eTag = GetEtagFromResponse(response);
            }

            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid, eTag))
            {
                await ValidateResponseMetadataDatasetAsync(response, firstStoredInstance, secondStoredInstance);
            }
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenInstanceIsDeletedInSeriesAndPreviousETagIsUsed_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string firstSopInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Instance, studyInstanceUid, seriesInstanceUid, firstSopInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());
            string eTag;

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid))
            {
                eTag = GetEtagFromResponse(response);
            }

            await _client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, firstSopInstanceUid);

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid, eTag))
            {
                DicomDataset[] datasets = await response.ToArrayAsync();

                Assert.Single(datasets);
                ValidateResponseMetadataDataset(secondStoredInstance, datasets[0]);
            }
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequest_WhenIfNoneMatchMatchesETag_ThenNotModifiedResponseIsReturned()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            await PostDicomFileAsync(ResourceType.Instance, studyInstanceUid, seriesInstanceUid, sopInstanceUid, dataSet: GenerateNewDataSet());
            string eTag;

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid))
            {
                eTag = GetEtagFromResponse(response);
            }

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, eTag))
            {
                Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
                ValidateNoContent(response);
            }
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequest_WhenIfNoneMatchDoesnotMatchETag_ThenResponseMetadataIsReturnedWithNewETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomDataset storedInstance = await PostDicomFileAsync(ResourceType.Instance, studyInstanceUid, seriesInstanceUid, sopInstanceUid, dataSet: GenerateNewDataSet());
            string ifNoneMatch;
            string eTag;

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid))
            {
                ifNoneMatch = null;
                eTag = GetEtagFromResponse(response);

                if (!string.IsNullOrEmpty(eTag))
                {
                    ifNoneMatch = string.Concat("1", eTag);
                }
            }

            using (DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("application/dicom+json", response.ContentHeaders.ContentType.MediaType);

                DicomDataset[] datasets = await response.ToArrayAsync();

                Assert.Single(datasets);
                ValidateResponseMetadataDataset(storedInstance, datasets[0]);
            }
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequest_WhenIfNoneMatchIsNotPresent_ThenResponseMetadataIsReturnedWithETag()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomDataset storedInstance = await PostDicomFileAsync(ResourceType.Instance, studyInstanceUid, seriesInstanceUid, sopInstanceUid, dataSet: GenerateNewDataSet());

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/dicom+json", response.ContentHeaders.ContentType.MediaType);

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.Single(datasets);
            ValidateResponseMetadataDataset(storedInstance, datasets[0]);
        }

        private string GetEtagFromResponse(DicomWebAsyncEnumerableResponse<DicomDataset> response)
        {
            string eTag = null;

            if (response.ResponseHeaders.TryGetValues(HeaderNames.ETag, out IEnumerable<string> eTagValues))
            {
                if (eTagValues.Any())
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

            using DicomWebResponse<DicomDataset> response = await _client.StoreAsync(new[] { dicomFile });

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
            Assert.Equal(
                JsonSerializer.Serialize(expectedDataset, ClientSerializerOptions.Json),
                JsonSerializer.Serialize(retrievedDataset, ClientSerializerOptions.Json));
            Assert.Equal(expectedDataset.Count(), retrievedDataset.Count());
        }

        private async Task ValidateResponseMetadataDatasetAsync(
            DicomWebAsyncEnumerableResponse<DicomDataset> response,
            DicomDataset storedInstance1,
            DicomDataset storedInstance2)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/dicom+json", response.ContentHeaders.ContentType.MediaType);

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.Equal(2, datasets.Length);

            // Trim the stored dataset to the expected items in the response metadata dataset (remove non-supported value representations).
            DicomDataset expectedDataset1 = storedInstance1.CopyWithoutBulkDataItems();
            DicomDataset expectedDataset2 = storedInstance2.CopyWithoutBulkDataItems();

            DicomDataset retrievedDataset1 = datasets[0];
            DicomDataset retrievedDataset2 = datasets[1];

            // Compare result datasets by serializing.
            string serializedExpectedDataset1 = JsonSerializer.Serialize(expectedDataset1, ClientSerializerOptions.Json);
            string serializedExpectedDataset2 = JsonSerializer.Serialize(expectedDataset2, ClientSerializerOptions.Json);

            string serializedRetrievedDataset1 = JsonSerializer.Serialize(retrievedDataset1, ClientSerializerOptions.Json);
            string serializedRetrievedDataset2 = JsonSerializer.Serialize(retrievedDataset2, ClientSerializerOptions.Json);

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

        private void ValidateNoContent(DicomWebAsyncEnumerableResponse<DicomDataset> response)
        {
            Assert.Equal(0, response.ContentHeaders.ContentLength);
        }
    }
}
