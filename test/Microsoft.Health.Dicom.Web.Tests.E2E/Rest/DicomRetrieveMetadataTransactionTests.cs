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
using Microsoft.Health.Dicom.Core.Messages;
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
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveStudyMetadataAsync(TestUidGenerator.Generate()));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenSeriesInstanceUidDoesnotExists_ThenNotFoundIsReturned()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, TestUidGenerator.Generate());

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveSeriesMetadataAsync(studyInstanceUid, TestUidGenerator.Generate()));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenSopInstanceUidDoesnotExists_ThenNotFoundIsReturned()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();

            await PostDicomFileAsync(ResourceType.Instance, studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate());

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate()));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenStoredDicomFile_WhenRetrievingMetadataForStudy_ThenMetadataIsRetrievedCorrectly()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Study, studyInstanceUid, dataSet: GenerateNewDataSet());

            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid);

            ValidateResponseMetadataDataset(response, firstStoredInstance, secondStoredInstance);
        }

        [Fact]
        public async Task GivenStoredDicomFile_WhenRetrievingMetadataForSeries_ThenMetadataIsRetrievedCorrectly()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();

            DicomDataset firstStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());
            DicomDataset secondStoredInstance = await PostDicomFileAsync(ResourceType.Series, studyInstanceUid, seriesInstanceUid, dataSet: GenerateNewDataSet());

            DicomWebResponse<IReadOnlyList<DicomDataset>> response = await _client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid);

            ValidateResponseMetadataDataset(response, firstStoredInstance, secondStoredInstance);
        }

        [Fact]
        public async Task GivenStoredDicomFile_WhenRetrievingMetadataForInstance_ThenMetadataIsRetrievedCorrectly()
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

        [Fact]
        public async Task GivenStoredDicomFileWithInvalidVrValue_WhenMetadataIsRetrieved_ThenDicomWebExceptionIsThrown()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            // Disable autovalidation to store dicomfile with invalid VR value.
#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = false;
#pragma warning restore CS0618 // Type or member is obsolete

            await PostDicomFileAsync(ResourceType.Instance, studyInstanceUid, seriesInstanceUid, sopInstanceUid, dataSet: GenerateNewDataSetWithInvalidVR());

            // Enable autovalidation now to test retrieve
#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = true;
#pragma warning restore CS0618 // Type or member is obsolete

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        }

        private static DicomDataset GenerateNewDataSetWithInvalidVR()
        {
            var dicomDataset = new DicomDataset();

            dicomDataset.Add(DicomTag.SeriesDescription, "CT1 abdomen\u0000");

            return dicomDataset;
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
    }
}
