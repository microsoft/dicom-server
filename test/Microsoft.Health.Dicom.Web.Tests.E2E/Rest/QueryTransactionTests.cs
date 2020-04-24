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
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class QueryTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly DicomWebClient _client;
        private readonly ILogger<QueryTransactionTests> _logger;

        public QueryTransactionTests(HttpIntegrationTestFixture<Startup> fixture, ITestOutputHelper testOutputHelper)
        {
            _client = fixture.Client;
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(testOutputHelper));
            _logger = loggerFactory.CreateLogger<QueryTransactionTests>();

            _logger.LogInformation("Auth: ", _client.HttpClient.DefaultRequestHeaders.Authorization.ToString());
        }

        [Fact]
        public async Task GivenSearchRequest_WithUnsupportedTag_ReturnBadRequest()
        {
            DicomWebException<string> exception = await Assert.ThrowsAsync<DicomWebException<string>>(
                () => _client.QueryWithBadRequest("/studies?Modality=CT"));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
            Assert.Equal(exception.Value, string.Format(DicomCoreResource.UnsupportedSearchParameter, "Modality"));
        }

        [Fact]
        public async Task GivenSearchRequest_WithValidParamsAndNoMatchingResult_ReturnNoContent()
        {
            try
            {
                DicomWebResponse<IEnumerable<DicomDataset>> response = await _client.QueryAsync("/studies?StudyDate=20200101");
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
            catch (DicomWebException e)
            {
                _logger.LogError(e, "Failed msg");
            }
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

            DicomWebResponse<IEnumerable<DicomDataset>> response = await _client.QueryAsync(
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

            DicomWebResponse<IEnumerable<DicomDataset>> response = await _client.QueryAsync(
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

            DicomWebResponse<IEnumerable<DicomDataset>> response = await _client.QueryAsync(
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

            DicomWebResponse<IEnumerable<DicomDataset>> response = await _client.QueryAsync(
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

            DicomWebResponse<IEnumerable<DicomDataset>> response = await _client.QueryAsync(
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

            DicomWebResponse<IEnumerable<DicomDataset>> response = await _client.QueryAsync(
                   $"/instances?Modality=XRAY");

            Assert.NotNull(response.Value);
            DicomDataset testDataResponse = response.Value.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId);
            Assert.NotNull(testDataResponse);
            ValidateResponseDataset(QueryResource.AllInstances, matchInstance, testDataResponse);
        }

        [Fact]
        public async Task GivenSearchRequest_PatientNameFuzzyMatch_MatchResult()
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
            DicomWebResponse<IEnumerable<DicomDataset>> response = null;
            while (retryCount < 3 || testDataResponse1 == null)
            {
                response = await _client.QueryAsync(
                       $"/studies?PatientName={randomNamePart}&FuzzyMatching=true");

                testDataResponse1 = response.Value?.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId1);
                retryCount++;
            }

            Assert.NotNull(testDataResponse1);
            ValidateResponseDataset(QueryResource.AllStudies, matchInstance1, testDataResponse1);

            DicomDataset testDataResponse2 = response.Value.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId2);
            Assert.NotNull(testDataResponse2);
            ValidateResponseDataset(QueryResource.AllStudies, matchInstance2, testDataResponse2);
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

#pragma warning disable SA1402 // File may only contain a single type
    public class XunitLoggerProvider : ILoggerProvider
#pragma warning restore SA1402 // File may only contain a single type
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public XunitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
            => new XunitLogger(_testOutputHelper, categoryName);

        public void Dispose()
        {
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
    public class XunitLogger : ILogger
#pragma warning restore SA1402 // File may only contain a single type
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _categoryName;

        public XunitLogger(ITestOutputHelper testOutputHelper, string categoryName)
        {
            _testOutputHelper = testOutputHelper;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
            => NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _testOutputHelper.WriteLine($"{_categoryName} [{eventId}] {formatter(state, exception)}");
            if (exception != null)
            {
                _testOutputHelper.WriteLine(exception.ToString());
            }
        }

        private class NoopDisposable : IDisposable
        {
#pragma warning disable SA1401 // Fields should be private
            public static NoopDisposable Instance = new NoopDisposable();
#pragma warning restore SA1401 // Fields should be private

            public void Dispose()
            {
            }
        }
    }
}
