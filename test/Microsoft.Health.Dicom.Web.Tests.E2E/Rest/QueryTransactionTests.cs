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
using Microsoft.Health.Dicom.Metadata.Config;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class QueryTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly DicomMetadataConfiguration _configuration = new DicomMetadataConfiguration();

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
        [InlineData("00080020", "19900215")]
        [InlineData("00080020", "19900215", "19900214-19900216")]
        [InlineData("00080020", "19900215", "19900215-19900215")]
        [InlineData("00080050", "50")]
        [InlineData("00080060", "MR")]
        [InlineData("00080060", "CT", "CT", "00080061")]
        [InlineData("00080090", "Mr^Test^Referring")]
        [InlineData("00100010", "Mr^Test^Patient")]
        [InlineData("00100020", "5")]
        public async Task GivenAStudy_WhenQueryingStudy_TheServerShouldReturnStudy(
            string attributeId, string attributeValue, string queryValue = null, string queryAttributeId = null)
        {
            var studyInstanceUID = Guid.NewGuid().ToString();

            // var seriesInstanceUID = Guid.NewGuid().ToString();
            var dicomAttributeId = new DicomAttributeId(attributeId);
            var optionalItems = new DicomDataset { { dicomAttributeId, attributeValue } };

            await CreateRandomQueryDatasetAsync(studyInstanceUID: studyInstanceUID, numberOfInstances: 5, optionalItems: optionalItems);

            DicomAttributeId queryAttribute = queryAttributeId == null ? dicomAttributeId : new DicomAttributeId(queryAttributeId);
            HttpResult<IReadOnlyList<DicomDataset>> queryResponse = await Client.QueryStudiesAsync(
                queryTags: new[] { (queryAttribute, queryValue ?? attributeValue) });
            Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);

            // We check for at least one result and the stored result exists. More items might exist on the server from other test runs.
            Assert.True(queryResponse.Value.Count >= 1);
            Assert.NotNull(queryResponse.Value.First(x => x.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyInstanceUID));
        }

        [Theory]
        [InlineData("00080020", "19900215")]
        [InlineData("00080020", "19900215", "19900214-19900216")]
        [InlineData("00080020", "19900215", "19900215-19900215")]
        [InlineData("00080050", "50")]
        [InlineData("00080060", "MR")]
        [InlineData("00080060", "CT", "CT", "00080061")]
        [InlineData("00080090", "Mr^Test^Referring")]
        [InlineData("00100010", "Mr^Test^Patient")]
        [InlineData("00100020", "5")]
        public async Task GivenASeries_WhenQueryingSeries_TheServerShouldReturnSeries(
            string attributeId, string attributeValue, string queryValue = null, string queryAttributeId = null)
        {
            const int numberOfSeries = 3;
            var studyInstanceUID = Guid.NewGuid().ToString();
            var dicomAttributeId = new DicomAttributeId(attributeId);
            var optionalItems = new DicomDataset { { dicomAttributeId, attributeValue } };

            DicomDataset[] datasets = await CreateRandomQueryDatasetAsync(studyInstanceUID, optionalItems: optionalItems, numberOfInstances: numberOfSeries);

            DicomAttributeId queryAttribute = queryAttributeId == null ? dicomAttributeId : new DicomAttributeId(queryAttributeId);
            HttpResult<IReadOnlyList<DicomDataset>> queryResponse = await Client.QuerySeriesAsync(
                studyInstanceUID: studyInstanceUID, queryTags: new[] { (queryAttribute, queryValue ?? attributeValue) });
            Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);
            Assert.Equal(numberOfSeries, queryResponse.Value.Count);

            HashSet<DicomAttributeId> requiredAttributes = _configuration.SeriesRequiredMetadataAttributes;
            requiredAttributes.Add(new DicomAttributeId(DicomTag.SeriesInstanceUID));
            requiredAttributes.Add(new DicomAttributeId(DicomTag.NumberOfSeriesRelatedInstances));

            foreach (DicomDataset responseDataset in queryResponse.Value)
            {
                DicomDataset expectedDataset = datasets.First(x => x.GetSingleValue<string>(DicomTag.SeriesInstanceUID) == responseDataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID));
                expectedDataset.Add(DicomTag.NumberOfSeriesRelatedInstances, 1);

                ValidateResponseQueryDataset(expectedDataset, responseDataset, requiredAttributes);
                ValidateResponseQueryDataset(expectedDataset, responseDataset, requiredAttributes);
            }
        }

        [Theory]
        [InlineData("00080020", "19900215", "20190219")]
        public async Task GivenMultipleInstancesWithInconsistentAttributes_WhenQueryingSeries_TheServerShouldReturnSeries(
            string attributeId, params string[] inconsistentValues)
        {
            var dicomAttributeId = new DicomAttributeId(attributeId);
            var studyInstanceUID = Guid.NewGuid().ToString();
            var seriesInstanceUID = Guid.NewGuid().ToString();

            foreach (var inconsistentValue in inconsistentValues)
            {
                await CreateRandomQueryDatasetAsync(
                    studyInstanceUID,
                    seriesInstanceUID,
                    optionalItems: new DicomDataset { { dicomAttributeId, inconsistentValue } });
            }

            foreach (var inconsistentValue in inconsistentValues)
            {
                HttpResult<IReadOnlyList<DicomDataset>> queryResponse = await Client.QuerySeriesAsync(
                    studyInstanceUID: studyInstanceUID, queryTags: new[] { (dicomAttributeId, inconsistentValue) });
                Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);
                Assert.Equal(1, queryResponse.Value.Count);
                Assert.Equal(seriesInstanceUID, queryResponse.Value[0].GetSingleValue<string>(DicomTag.SeriesInstanceUID));
                Assert.Equal(inconsistentValues.Length, queryResponse.Value[0].GetSingleValue<int>(DicomTag.NumberOfSeriesRelatedInstances));
            }
        }

        [Theory]
        [InlineData("00080020", "19900215")]
        [InlineData("00080020", "19900215", "19900214-19900216")]
        [InlineData("00080020", "19900215", "19900215-19900215")]
        [InlineData("00080050", "50")]
        [InlineData("00080060", "MR")]
        [InlineData("00080060", "CT", "CT", "00080061")]
        [InlineData("00080090", "Mr^Test^Referring")]
        [InlineData("00100010", "Mr^Test^Patient")]
        [InlineData("00100020", "5")]
        public async Task GivenAnInstance_WhenQueryingInstance_TheServerShouldReturnInstance(
            string attributeId, string attributeValue, string queryValue = null, string queryAttributeId = null)
        {
            var studyInstanceUID = Guid.NewGuid().ToString();
            var dicomAttributeId = new DicomAttributeId(attributeId);
            var optionalItems = new DicomDataset { { dicomAttributeId, attributeValue } };

            DicomDataset[] datasets = await CreateRandomQueryDatasetAsync(studyInstanceUID, optionalItems: optionalItems);

            DicomAttributeId queryAttribute = queryAttributeId == null ? dicomAttributeId : new DicomAttributeId(queryAttributeId);
            HttpResult<IReadOnlyList<DicomDataset>> queryResponse = await Client.QueryInstancesAsync(
                studyInstanceUID: studyInstanceUID, queryTags: new[] { (queryAttribute, queryValue ?? attributeValue) });
            Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);
            Assert.Equal(datasets.Length, queryResponse.Value.Count);

            HashSet<DicomAttributeId> requiredAttributes = _configuration.InstanceRequiredMetadataAttributes;
            requiredAttributes.Add(new DicomAttributeId(DicomTag.SOPInstanceUID));

            ValidateResponseQueryDataset(datasets[0], queryResponse.Value[0], requiredAttributes);
        }

        private static void ValidateResponseQueryDataset(
            DicomDataset originalDataset, DicomDataset queryDataset, HashSet<DicomAttributeId> expectedAttributes)
        {
            Assert.Equal(expectedAttributes.Count, queryDataset.Count());

            foreach (DicomAttributeId attributeId in expectedAttributes)
            {
                queryDataset.TryGetValues(attributeId, out string[] actualItems);
                originalDataset.TryGetValues(attributeId, out string[] expectedItems);

                if (actualItems == null && expectedItems == null)
                {
                    continue;
                }

                Assert.Equal(expectedItems.Length, actualItems.Length);
                for (var i = 0; i < actualItems.Length; i++)
                {
                    Assert.Equal(expectedItems[i], actualItems[i]);
                }
            }
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

        private async Task<DicomDataset[]> CreateRandomQueryDatasetAsync(string studyInstanceUID = null, string seriesInstanceUID = null, int numberOfInstances = 1, DicomDataset optionalItems = null)
        {
            var indexedAttributes = new DicomDataset()
            {
                { DicomTag.PatientName, Guid.NewGuid().ToString() },
                { DicomTag.PatientID, Guid.NewGuid().ToString() },
                { DicomTag.ReferringPhysicianName, Guid.NewGuid().ToString() },
                { DicomTag.StudyDate, DateTime.UtcNow.AddDays(-5) },
                { DicomTag.Modality, "CT" },
                { DicomTag.TimezoneOffsetFromUTC, "-0200" },
                { DicomTag.SpecificCharacterSet, "ISO 646" },
                { DicomTag.InstanceNumber, 5 },
                { DicomTag.SeriesDescription, "Test Description" },
                { DicomTag.PerformedProcedureStepStartDate, DateTime.UtcNow.AddDays(-5) },
                { DicomTag.PerformedProcedureStepStartTime, DateTime.UtcNow },
                { new DicomSequence(DicomTag.RequestAttributesSequence, new DicomDataset() { { DicomTag.AccessionNumber, "123" } }) },
            };

            if (optionalItems != null)
            {
                indexedAttributes.AddOrUpdate(optionalItems);
            }

            DicomFile[] dicomFiles = Enumerable.Range(0, numberOfInstances).Select(_ =>
            {
                DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUID, seriesInstanceUID, frames: 2);
                dicomFile.Dataset.AddOrUpdate(indexedAttributes);
                return dicomFile;
            }).ToArray();
            DicomDataset[] datasets = dicomFiles.Select(x => x.Dataset).ToArray();

            HttpResult<DicomDataset> response = await Client.PostAsync(dicomFiles);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            DicomSequence successSequence = response.Value.GetSequence(DicomTag.ReferencedSOPSequence);
            ValidationHelpers.ValidateSuccessSequence(successSequence, datasets);

            return datasets;
        }
    }
}
