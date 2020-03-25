// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using Dicom.Serialization;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class QueryTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly DicomWebClient _client;
        private object retrievedDataset;

        public QueryTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task GivenSearchRequest_WithValidParamsAndNoMatchingResult_ReturnNoContent()
        {
            HttpResult<IEnumerable<DicomDataset>> response = await _client.QueryAsync("/studies?StudyDate=20200101");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task GivenSearchRequest_AllStudyLevel_MatchResult()
        {
            // Add 2 study files, One matches the search and other does not
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.PatientName, "Test^Patient" },
                 { DicomTag.StudyDate, "20190101" },
            });
            DicomDataset unMatchedInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.PatientName, "Anonymous" },
                 { DicomTag.StudyDate, "20190101" },
            });
            HttpResult<IEnumerable<DicomDataset>> response = await _client.QueryAsync("/studies?StudyDate=20190101&PatientName=\"Test^Patient\"");
            Assert.Single(response.Value);

            ValidateResponseDataset(matchInstance, response.Value.Single());
        }

        private void ValidateResponseDataset(DicomDataset storedInstance, DicomDataset responseInstance)
        {
            DicomDataset expectedDataset = storedInstance.Clone();
            DicomMetadata.RemoveBulkDataVRs(expectedDataset);

            // Compare result datasets by serializing.
            var jsonDicomConverter = new JsonDicomConverter();
            Assert.Equal(
                JsonConvert.SerializeObject(expectedDataset, jsonDicomConverter),
                JsonConvert.SerializeObject(responseInstance, jsonDicomConverter));
            Assert.Equal(expectedDataset.Count(), responseInstance.Count());
        }

        [Fact]
        public async Task GivenSearchRequest_StudySeriesLevel_MatchResult()
        {
            await Task.FromResult(0);
        }

        [Fact]
        public async Task GivenSearchRequest_AllSeriesLevel_MatchResult()
        {
            await Task.FromResult(0);
        }

        [Fact]
        public async Task GivenSearchRequest_StudyInstancesLevel_MatchResult()
        {
            await Task.FromResult(0);
        }

        [Fact]
        public async Task GivenSearchRequest_StudySeriesInstancesLevel_MatchResult()
        {
            await Task.FromResult(0);
        }

        [Fact]
        public async Task GivenSearchRequest_AllIntancesLevel_MatchResult()
        {
            await Task.FromResult(0);
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
