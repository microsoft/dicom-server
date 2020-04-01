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
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class QueryTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly DicomWebClient _client;

        public QueryTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task GivenSearchRequest_WithUnsupportedTag_ReturnBadRequest()
        {
            HttpResult<string> response = await _client.QueryWithStringResponseAsync("/studies?Modality=CT");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(response.Value, string.Format(DicomCoreResource.UnsupportedSearchParameter, "Modality"));
        }

        [Fact]
        public async Task GivenSearchRequest_WithInvalidUid_ReturnBadRequest()
        {
            HttpResult<string> response = await _client.QueryWithStringResponseAsync("/studies/abcd.123/series");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(response.Value, string.Format(DicomCoreResource.DicomIdentifierInvalid, "studyInstanceUid", "abcd.123"));
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
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.StudyDate, "20190101" },
            });
            DicomDataset unMatchedInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.StudyDate, "20190102" },
            });
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            HttpResult<IEnumerable<DicomDataset>> response = await _client.QueryAsync(
                $"/studies?StudyDate=20190101");

            Assert.NotNull(response.Value);
            DicomDataset testDataResponse = response.Value.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId);
            Assert.NotNull(testDataResponse);
            ValidateResponseDataset(QueryResource.AllStudies, matchInstance, testDataResponse);
        }

        [Fact]
        public async Task GivenSearchRequest_StudySeriesLevel_MatchResult()
        {
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.Modality, "MRI" },
            });
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.StudyInstanceUID, studyId },
                 { DicomTag.Modality, "CT" },
            });

            HttpResult<IEnumerable<DicomDataset>> response = await _client.QueryAsync(
                $"/studies/{studyId}/series?Modality=MRI");

            Assert.Single(response.Value);
            ValidateResponseDataset(QueryResource.StudySeries, matchInstance, response.Value.Single());
        }

        [Fact]
        public async Task GivenSearchRequest_AllSeriesLevel_MatchResult()
        {
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.Modality, "MRI" },
            });
            var seriesId = matchInstance.GetSingleValue<string>(DicomTag.SeriesInstanceUID);

            HttpResult<IEnumerable<DicomDataset>> response = await _client.QueryAsync(
                $"/series?Modality=MRI");

            Assert.NotNull(response.Value);
            DicomDataset testDataResponse = response.Value.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.SeriesInstanceUID) == seriesId);
            Assert.NotNull(testDataResponse);
            ValidateResponseDataset(QueryResource.AllSeries, matchInstance, testDataResponse);
        }

        [Fact]
        public async Task GivenSearchRequest_StudyInstancesLevel_MatchResult()
        {
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.Modality, "MRI" },
            });
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.StudyInstanceUID, studyId },
                 { DicomTag.Modality, "CT" },
            });

            HttpResult<IEnumerable<DicomDataset>> response = await _client.QueryAsync(
                   $"/studies/{studyId}/instances?Modality=MRI");

            Assert.Single(response.Value);
            ValidateResponseDataset(QueryResource.StudyInstances, matchInstance, response.Value.Single());
        }

        [Fact]
        public async Task GivenSearchRequest_StudySeriesInstancesLevel_MatchResult()
        {
            DicomDataset matchInstance = await PostDicomFileAsync();
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            var seriesId = matchInstance.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            var instanceId = matchInstance.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.StudyInstanceUID, studyId },
                 { DicomTag.SeriesInstanceUID, seriesId },
            });

            HttpResult<IEnumerable<DicomDataset>> response = await _client.QueryAsync(
                   $"/studies/{studyId}/series/{seriesId}/instances?SOPInstanceUID={instanceId}");

            Assert.Single(response.Value);
            ValidateResponseDataset(QueryResource.StudySeriesInstances, matchInstance, response.Value.Single());
        }

        [Fact]
        public async Task GivenSearchRequest_AllIntancesLevel_MatchResult()
        {
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.Modality, "XRAY" },
            });
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            HttpResult<IEnumerable<DicomDataset>> response = await _client.QueryAsync(
                   $"/instances?Modality=XRAY");

            Assert.NotNull(response.Value);
            DicomDataset testDataResponse = response.Value.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId);
            Assert.NotNull(testDataResponse);
            ValidateResponseDataset(QueryResource.AllInstances, matchInstance, testDataResponse);
        }

        private async Task<DicomDataset> PostDicomFileAsync(DicomDataset metadataItems = null)
        {
            DicomFile dicomFile1 = CreateDicomFile();

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

        private static DicomFile CreateDicomFile()
        {
            return new DicomFile(new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
            {
                { DicomTag.StudyInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.PatientID, TestUidGenerator.Generate() },
                { DicomTag.PatientName, "Query^Test^Patient" },
                { DicomTag.StudyDate, "20080701" },
                { DicomTag.SeriesInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.Modality, "CT" },
                { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SOPClassUID, TestUidGenerator.Generate() },
                { DicomTag.BitsAllocated, (ushort)8 },
            });
        }

        private void ValidateResponseDataset(
            QueryResource resource,
            DicomDataset storedInstance,
            DicomDataset responseInstance)
        {
            DicomDataset expectedDataset = storedInstance.Clone();
            HashSet<DicomTag> levelTags = new HashSet<DicomTag>();
            switch (resource)
            {
                case QueryResource.AllStudies:
                    levelTags.Add(DicomTag.StudyInstanceUID);
                    levelTags.Add(DicomTag.PatientID);
                    levelTags.Add(DicomTag.PatientName);
                    levelTags.Add(DicomTag.StudyDate);
                    break;
                case QueryResource.AllSeries:
                    levelTags.Add(DicomTag.StudyInstanceUID);
                    levelTags.Add(DicomTag.PatientID);
                    levelTags.Add(DicomTag.PatientName);
                    levelTags.Add(DicomTag.StudyDate);
                    levelTags.Add(DicomTag.SeriesInstanceUID);
                    levelTags.Add(DicomTag.Modality);
                    break;
                case QueryResource.AllInstances:
                    levelTags.Add(DicomTag.StudyInstanceUID);
                    levelTags.Add(DicomTag.PatientID);
                    levelTags.Add(DicomTag.PatientName);
                    levelTags.Add(DicomTag.StudyDate);
                    levelTags.Add(DicomTag.SeriesInstanceUID);
                    levelTags.Add(DicomTag.Modality);
                    levelTags.Add(DicomTag.SOPInstanceUID);
                    levelTags.Add(DicomTag.SOPClassUID);
                    levelTags.Add(DicomTag.BitsAllocated);
                    break;
                case QueryResource.StudySeries:
                    levelTags.Add(DicomTag.StudyInstanceUID);
                    levelTags.Add(DicomTag.SeriesInstanceUID);
                    levelTags.Add(DicomTag.Modality);
                    break;
                case QueryResource.StudyInstances:
                    levelTags.Add(DicomTag.StudyInstanceUID);
                    levelTags.Add(DicomTag.SeriesInstanceUID);
                    levelTags.Add(DicomTag.Modality);
                    levelTags.Add(DicomTag.SOPInstanceUID);
                    levelTags.Add(DicomTag.SOPClassUID);
                    levelTags.Add(DicomTag.BitsAllocated);
                    break;
                case QueryResource.StudySeriesInstances:
                    levelTags.Add(DicomTag.StudyInstanceUID);
                    levelTags.Add(DicomTag.SeriesInstanceUID);
                    levelTags.Add(DicomTag.SOPInstanceUID);
                    levelTags.Add(DicomTag.SOPClassUID);
                    levelTags.Add(DicomTag.BitsAllocated);
                    break;
            }

            expectedDataset.Remove((di) =>
            {
                return !levelTags.Contains(di.Tag);
            });

            // Compare result datasets by serializing.
            var jsonDicomConverter = new JsonDicomConverter();
            Assert.Equal(
                JsonConvert.SerializeObject(expectedDataset, jsonDicomConverter),
                JsonConvert.SerializeObject(responseInstance, jsonDicomConverter));
            Assert.Equal(expectedDataset.Count(), responseInstance.Count());
        }
    }
}
