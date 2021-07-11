// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dicom;
using Dicom.Serialization;
using EnsureThat;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Tests.Common;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class QueryTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>, IDisposable
    {
        private readonly IDicomWebClient _client;
        private readonly HashSet<string> _createdDicomStudies = new HashSet<string>();

        public QueryTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _client = fixture.Client;
        }

        [Theory]
        [ClassData(typeof(VersionAPIData))]
        public async Task GivenSearchRequest_WithUnsupportedTag_ReturnBadRequest(string versionPath)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => _client.QueryAsync(new Uri($"{versionPath}/studies?Modality=CT", UriKind.Relative)));

            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
            Assert.Equal(exception.ResponseMessage, string.Format(DicomCoreResource.UnsupportedSearchParameter, "Modality", "study"));
        }

        [Theory]
        [ClassData(typeof(VersionAPIData))]
        public async Task GivenSearchRequest_WithValidParamsAndNoMatchingResult_ReturnNoContent(string versionPath)
        {
            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryAsync(new Uri($"{versionPath}/studies?StudyDate=20200101", UriKind.Relative));
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Theory]
        [ClassData(typeof(VersionAPIData))]
        [Trait("Category", "bvt")]
        public async Task GivenSearchRequest_AllStudyLevel_MatchResult(string versionPath)
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

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryAsync(new Uri($"{versionPath}/studies?StudyDate=20190101", UriKind.Relative));

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.NotEmpty(datasets);
            DicomDataset testDataResponse = datasets.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId);
            Assert.NotNull(testDataResponse);
            ValidateResponseDataset(QueryResource.AllStudies, matchInstance, testDataResponse);
        }

        [Theory]
        [ClassData(typeof(VersionAPIData))]
        public async Task GivenSearchRequest_AllStudyLevelOnPatientName_MatchIsCaseIncensitiveAndAccentIncensitive(string versionPath)
        {
            string randomNamePart = RandomString(7);
            string patientName = $"Hall^{randomNamePart}^Tá";
            string patientNameWithNoAccent = $"Hall^{randomNamePart}^Ta";

            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                { DicomTag.PatientName, Encoding.UTF8, patientName },
                { DicomTag.SpecificCharacterSet, "ISO_IR 192" },
            });

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryAsync(
                new Uri($"{versionPath}/studies?PatientName={patientNameWithNoAccent}", UriKind.Relative));

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.Single(datasets);
            DicomDataset testDataResponse = datasets[0];
            Assert.NotNull(testDataResponse);
            Assert.Equal(patientName, testDataResponse.GetString(DicomTag.PatientName));
        }

        [Theory]
        [ClassData(typeof(VersionAPIData))]
        public async Task GivenSearchRequest_StudySeriesLevel_MatchResult(string versionPath)
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

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryAsync(
                new Uri($"{versionPath}/studies/{studyId}/series?Modality=MRI", UriKind.Relative));

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.Single(datasets);
            ValidateResponseDataset(QueryResource.StudySeries, matchInstance, datasets[0]);
        }

        [Theory]
        [ClassData(typeof(VersionAPIData))]
        public async Task GivenSearchRequest_AllSeriesLevel_MatchResult(string versionPath)
        {
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.Modality, "MRI" },
            });
            var seriesId = matchInstance.GetSingleValue<string>(DicomTag.SeriesInstanceUID);

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryAsync(
                new Uri($"{versionPath}/series?Modality=MRI", UriKind.Relative));

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.NotNull(datasets);
            DicomDataset testDataResponse = datasets.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.SeriesInstanceUID) == seriesId);
            Assert.NotNull(testDataResponse);
            ValidateResponseDataset(QueryResource.AllSeries, matchInstance, testDataResponse);
        }

        [Theory]
        [ClassData(typeof(VersionAPIData))]
        public async Task GivenSearchRequest_StudyInstancesLevel_MatchResult(string versionPath)
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

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryAsync(
                   new Uri($"{versionPath}/studies/{studyId}/instances?Modality=MRI", UriKind.Relative));

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.Single(datasets);
            ValidateResponseDataset(QueryResource.StudyInstances, matchInstance, datasets[0]);
        }

        [Theory]
        [ClassData(typeof(VersionAPIData))]
        public async Task GivenSearchRequest_StudySeriesInstancesLevel_MatchResult(string versionPath)
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

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryAsync(
                new Uri($"{versionPath}/studies/{studyId}/series/{seriesId}/instances?SOPInstanceUID={instanceId}", UriKind.Relative));

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.Single(datasets);
            ValidateResponseDataset(QueryResource.StudySeriesInstances, matchInstance, datasets[0]);
        }

        [Theory]
        [ClassData(typeof(VersionAPIData))]
        public async Task GivenSearchRequest_AllInstancesLevel_MatchResult(string versionPath)
        {
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.Modality, "XRAY" },
            });
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryAsync(
                new Uri($"{versionPath}/instances?Modality=XRAY", UriKind.Relative));

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.NotNull(datasets);
            DicomDataset testDataResponse = datasets.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId);
            Assert.NotNull(testDataResponse);
            ValidateResponseDataset(QueryResource.AllInstances, matchInstance, testDataResponse);
        }

        [Theory]
        [ClassData(typeof(VersionAPIData))]
        public async Task GivenSearchRequest_PatientNameFuzzyMatch_MatchResult(string versionPath)
        {
            string randomNamePart = RandomString(7);
            DicomDataset matchInstance2 = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.PatientName, $"Jonathan^{randomNamePart}^Stone Hall^^" },
            });
            var studyId2 = matchInstance2.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            DicomDataset matchInstance1 = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.PatientName, $"Jon^{randomNamePart}^StoneHall" },
            });
            var studyId1 = matchInstance1.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            // Retrying the query 3 times, to give sql FT index time to catch up
            int retryCount = 0;
            DicomDataset testDataResponse1 = null;
            DicomDataset[] responseDatasets = null;

            while (retryCount < 3 || testDataResponse1 == null)
            {
                using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryAsync(
                    new Uri($"{versionPath}/studies?PatientName={randomNamePart}&FuzzyMatching=true", UriKind.Relative));

                responseDatasets = await response.ToArrayAsync();

                testDataResponse1 = responseDatasets?.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId1);
                retryCount++;
            }

            Assert.NotNull(testDataResponse1);
            ValidateResponseDataset(QueryResource.AllStudies, matchInstance1, testDataResponse1);

            DicomDataset testDataResponse2 = responseDatasets.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId2);
            Assert.NotNull(testDataResponse2);
            ValidateResponseDataset(QueryResource.AllStudies, matchInstance2, testDataResponse2);
        }

        [Theory]
        [ClassData(typeof(VersionAPIData))]
        public async Task GivenSearchRequest_ReferringPhysicianNameFuzzyMatch_MatchResult(string versionPath)
        {
            string randomNamePart = RandomString(7);
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.PatientName, $"dr^{randomNamePart}^Stone Hall^^" },
            });
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            // Retrying the query 3 times, to give sql FT index time to catch up
            int retryCount = 0;
            DicomDataset testDataResponse = null;
            DicomDataset[] responseDatasets = null;

            while (retryCount < 3 || testDataResponse == null)
            {
                using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryAsync(
                    new Uri($"{versionPath}/studies?PatientName={randomNamePart}&FuzzyMatching=true", UriKind.Relative));

                responseDatasets = await response.ToArrayAsync();

                testDataResponse = responseDatasets?.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId);
                retryCount++;
            }

            Assert.NotNull(testDataResponse);
            ValidateResponseDataset(QueryResource.AllStudies, matchInstance, testDataResponse);
        }

        [Theory]
        [ClassData(typeof(VersionAPIData))]
        public async Task GivenSearchRequest_OHIFViewerStudyQuery_ReturnsOK(string versionPath)
        {
            var OhifViewerQuery = $"{versionPath}/studies?limit=25&offset=0&includefield=00081030%2C00080060&StudyDate=19521125-20210507";

            // client is checking the success response and throws exception otherwise
            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryAsync(new Uri(OhifViewerQuery, UriKind.Relative));
        }

        private static string RandomString(int length)
        {
            var random = new Random();

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task<DicomDataset> PostDicomFileAsync(DicomDataset metadataItems = null)
        {
            DicomFile dicomFile1 = CreateDicomFile();

            if (metadataItems != null)
            {
                dicomFile1.Dataset.AddOrUpdate(metadataItems);
            }

            await _client.StoreAsync(new[] { dicomFile1 });

            _createdDicomStudies.Add(dicomFile1.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));

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

        void IDisposable.Dispose()
        {
            // xunit does not seem to call IAsyncDispose.DisposeAsync()
            // Also wait should be okay in a test context
            foreach (string studyUid in _createdDicomStudies)
            {
                _client.DeleteStudyAsync(studyUid).Wait();
            }

            _createdDicomStudies.Clear();
        }
    }
}
