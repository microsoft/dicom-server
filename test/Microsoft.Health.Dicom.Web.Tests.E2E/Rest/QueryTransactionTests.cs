// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class QueryTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        public QueryTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            Client = new DicomWebClient(fixture.HttpClient);
        }

        protected DicomWebClient Client { get; set; }

        [Theory]
        [InlineData("application/data")]
        [InlineData("application/dicom")]
        public async Task GivenAnIncorrectAcceptHeader_WhenQuerying_TheServerShouldReturnNotAcceptable(string acceptHeader)
        {
            var requestUris = new string[]
            {
                "studies?fuzzymatching=true",
                "series?fuzzymatching=true",
                $"studies/{Guid.NewGuid().ToString()}/series?fuzzymatching=true",
                "instances?fuzzymatching=true",
                $"studies/{Guid.NewGuid().ToString()}/instances?fuzzymatching=true",
                $"studies/{Guid.NewGuid().ToString()}/series/{Guid.NewGuid().ToString()}/instances?fuzzymatching=true",
            };

            foreach (var requestUri in requestUris)
            {
                await AssertQueryFailureStatusCodeAsync(requestUri, HttpStatusCode.NotAcceptable, acceptHeader);
            }
        }

        [Theory]
        [InlineData("unknown1", "unknown2")]
        public async Task GivenAnUnknownInstanceIdentifier_WhenQueryingSeriesOrInstances_TheServerShouldReturnOKandNoResults(string studyInstanceUID, string seriesInstanceUID)
        {
            HttpResult<IReadOnlyList<DicomDataset>> queryResponse = await Client.QuerySeriesAsync(studyInstanceUID);
            Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);
            Assert.Empty(queryResponse.Value);

            queryResponse = await Client.QueryInstancesAsync(studyInstanceUID);
            Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);
            Assert.Empty(queryResponse.Value);

            queryResponse = await Client.QueryInstancesAsync(studyInstanceUID, seriesInstanceUID);
            Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);
            Assert.Empty(queryResponse.Value);
        }

        [Theory]
        [InlineData("19900215")]
        public async Task GivenAnInstance_WhenQueryingUsingStudyDate_TheServerShouldReturnInstance(string queryValue)
        {
            await CreateRandomQueryDatasetAsync(optionalItems: new DicomDataset()
            {
                { DicomTag.StudyDate, new DateTime(1990, 2, 15) },
            });

            HttpResult<IReadOnlyList<DicomDataset>> queryResponse = await Client.QueryInstancesAsync(queryTags: new[] { (new DicomAttributeId(DicomTag.StudyDate), queryValue) }, optionalDicomAttributes: new[] { "all" });
            Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);
            Assert.Single(queryResponse.Value);
        }

        private async Task AssertQueryFailureStatusCodeAsync(string requestUri, HttpStatusCode expectedStatusCode, string acceptHeader = "application/dicom+json")
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add(HeaderNames.Accept, acceptHeader);

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(expectedStatusCode, response.StatusCode);
            }
        }

        private async Task<DicomDataset[]> CreateRandomQueryDatasetAsync(int numberOfInstances = 1, DicomDataset optionalItems = null)
        {
            var indexedAttributes = new DicomDataset()
            {
                { DicomTag.PatientName, Guid.NewGuid().ToString() },
                { DicomTag.PatientID, Guid.NewGuid().ToString() },
                { DicomTag.ReferringPhysicianName, Guid.NewGuid().ToString() },
                { DicomTag.StudyDate, DateTime.UtcNow.AddDays(-5) },
                { DicomTag.Modality, "CT" },
            };

            if (optionalItems != null)
            {
                indexedAttributes.AddOrUpdate(optionalItems);
            }

            DicomFile[] dicomFiles = Enumerable.Range(0, numberOfInstances).Select(_ =>
            {
                DicomFile dicomFile = Samples.CreateRandomDicomFile();
                dicomFile.Dataset.AddOrUpdate(indexedAttributes);
                return dicomFile;
            }).ToArray();
            var datasets = dicomFiles.Select(x => x.Dataset).ToArray();

            HttpResult<DicomDataset> response = await Client.PostAsync(dicomFiles);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            DicomSequence successSequence = response.Value.GetSequence(DicomTag.ReferencedSOPSequence);
            ValidationHelpers.ValidateSuccessSequence(successSequence, datasets);

            return datasets;
        }
    }
}
