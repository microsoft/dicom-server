// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class ExtendedQueryTagTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly IDicomWebClient _client;
        private const string PrivateCreatorName = "PrivateCreator1";
        public ExtendedQueryTagTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            EnsureArg.IsNotNull(fixture.Client, nameof(fixture.Client));
            _client = fixture.Client;
        }

        [Fact]
        public async Task GivenValidExtendedQueryTags_WhenGoThroughEndToEndScenario_ThenShouldSucceed()
        {
            // Prepare 3 extended query tags.
            // One is private tag on Instance level
            // To add private tag, need to add identification code element at first.
            DicomTag identificationCodeTag = new DicomTag(0x0407, 0x0010);

            DicomElement identificationCodeElement = new DicomLongString(identificationCodeTag, PrivateCreatorName);

            DicomTag privateTag = new DicomTag(0x0407, 0x1001, PrivateCreatorName);
            AddExtendedQueryTag privateQueryTag = new AddExtendedQueryTag { Path = privateTag.GetPath(), VR = DicomVRCode.SS, QueryTagLevel = QueryTagLevel.Instance.ToString(), PrivateCreator = privateTag.PrivateCreator.Creator };

            // One is standard tag on Series level
            DicomTag standardTagSeries = DicomTag.ManufacturerModelName;
            AddExtendedQueryTag standardTagSeriesQueryTag = new AddExtendedQueryTag { Path = standardTagSeries.GetPath(), VR = standardTagSeries.GetDefaultVR().Code, QueryTagLevel = QueryTagLevel.Series.ToString() };

            // One is standard tag on Study level
            DicomTag standardTagStudy = DicomTag.PatientSex;
            AddExtendedQueryTag standardTagStudyQueryTag = new AddExtendedQueryTag { Path = standardTagStudy.GetPath(), VR = standardTagStudy.GetDefaultVR().Code, QueryTagLevel = QueryTagLevel.Study.ToString() };

            AddExtendedQueryTag[] queryTags = new AddExtendedQueryTag[] { privateQueryTag, standardTagSeriesQueryTag, standardTagStudyQueryTag };

            // Create 3 test files on same studyUid.
            string studyUid = TestUidGenerator.Generate();
            string seriesUid1 = TestUidGenerator.Generate();
            string seriesUid2 = TestUidGenerator.Generate();
            string instanceUid1 = TestUidGenerator.Generate();
            string instanceUid2 = TestUidGenerator.Generate();
            string instanceUid3 = TestUidGenerator.Generate();

            // One is on seriesUid1 and instanceUid1
            DicomDataset dataset1 = Samples.CreateRandomInstanceDataset(studyInstanceUid: studyUid, seriesInstanceUid: seriesUid1, sopInstanceUid: instanceUid1);
            dataset1.Add(identificationCodeElement);
            dataset1.AddOrUpdate(new DicomSignedShort(privateTag, 1));
            dataset1.Add(standardTagSeries, "ManufacturerModelName1");
            dataset1.Add(standardTagStudy, "0");

            // One is on seriesUid1 and instanceUid2
            DicomDataset dataset2 = Samples.CreateRandomInstanceDataset(studyInstanceUid: studyUid, seriesInstanceUid: seriesUid1, sopInstanceUid: instanceUid2);
            dataset2.Add(identificationCodeElement);
            dataset2.AddOrUpdate(new DicomSignedShort(privateTag, 2));
            dataset2.Add(standardTagSeries, "ManufacturerModelName2");
            dataset2.Add(standardTagStudy, "0");

            // One is on seriesUid2 and instanceUid3
            DicomDataset dataset3 = Samples.CreateRandomInstanceDataset(studyInstanceUid: studyUid, seriesInstanceUid: seriesUid2, sopInstanceUid: instanceUid3);
            dataset3.Add(identificationCodeElement);
            dataset3.AddOrUpdate(new DicomSignedShort(privateTag, 3));
            dataset3.Add(standardTagSeries, "ManufacturerModelName3");
            dataset3.Add(standardTagStudy, "1");
            try
            {
                // Add extended query tags

                await _client.AddExtendedQueryTagAsync(queryTags);
                try
                {
                    foreach (var queryTag in queryTags)
                    {
                        GetExtendedQueryTag returnTag = await (await _client.GetExtendedQueryTagAsync(queryTag.Path)).GetValueAsync();
                        CompareExtendedQueryTagEntries(queryTag, returnTag);
                    }

                    // Upload test files
                    IEnumerable<DicomFile> dicomFiles = new DicomDataset[] { dataset1, dataset2, dataset3 }.Select(dataset => new DicomFile(dataset));

                    await _client.StoreAsync(dicomFiles, studyInstanceUid: string.Empty, cancellationToken: default);

                    // Query on instance for private tag
                    DicomWebAsyncEnumerableResponse<DicomDataset> queryInstanceResponse = await _client.QueryAsync($"/instances?{privateTag.GetPath()}=3", cancellationToken: default);
                    DicomDataset[] instanceResult = await queryInstanceResponse.ToArrayAsync();
                    Assert.Single(instanceResult);
                    Assert.Equal(instanceUid3, instanceResult[0].GetSingleValue<string>(DicomTag.SOPInstanceUID));

                    // Query on series for standardTagSeries
                    DicomWebAsyncEnumerableResponse<DicomDataset> querySeriesResponse = await _client.QueryAsync($"/series?{standardTagSeries.GetPath()}=ManufacturerModelName2", cancellationToken: default);
                    DicomDataset[] seriesResult = await querySeriesResponse.ToArrayAsync();
                    Assert.Single(seriesResult);
                    Assert.Equal(seriesUid1, seriesResult[0].GetSingleValue<string>(DicomTag.SeriesInstanceUID));

                    // Query on study for standardTagStudy
                    DicomWebAsyncEnumerableResponse<DicomDataset> queryStudyResponse = await _client.QueryAsync($"/studies?{standardTagStudy.GetPath()}=1", cancellationToken: default);
                    DicomDataset[] studyResult = await queryStudyResponse.ToArrayAsync();
                    Assert.Single(studyResult);
                    Assert.Equal(studyUid, seriesResult[0].GetSingleValue<string>(DicomTag.StudyInstanceUID));
                }
                finally
                {
                    await _client.DeleteStudyAsync(studyUid);
                }
            }
            finally
            {
                // Cleanup extended query tags, also verify GetExtendedQueryTagsAsync.
                var responseQueryTags = await (await _client.GetExtendedQueryTagsAsync()).GetValueAsync();
                foreach (var rTag in responseQueryTags)
                {
                    if (queryTags.Any(tag => tag.Path == rTag.Path))
                    {
                        await _client.DeleteExtendedQueryTagAsync(rTag.Path);
                    }
                }
            }
        }

        [Theory]
        [InlineData("[{\"Path\":\"00100040\"}]", "QueryTagLevel")]
        [InlineData("[{\"Path\":\"\",\"QueryTagLevel\":\"Study\"}]", "Path")]
        public async Task GivenMissingPropertyInRequestBody_WhenCallingPostAsync_ThenShouldThrowException(string requestBody, string missingProperty)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/extendedquerytags");
            {
                request.Content = new StringContent(requestBody);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
            }

            HttpResponseMessage response = await _client.HttpClient.SendAsync(request, default(CancellationToken))
                .ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(string.Format("The request body is not valid. Details: The Dicom Tag Property {0} must be specified and must not be null, empty or whitespace", missingProperty), response.Content.ReadAsStringAsync().Result);
        }

        [Fact]
        public async Task GivenInvalidTagLevelInRequestBody_WhenCallingPostAync_ThenShouldThrowException()
        {
            string requestBody = "[{\"Path\":\"00100040\",\"QueryTagLevel\":\"Studys\"}]";
            using var request = new HttpRequestMessage(HttpMethod.Post, "/extendedquerytags");
            {
                request.Content = new StringContent(requestBody);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
            }

            HttpResponseMessage response = await _client.HttpClient.SendAsync(request, default(CancellationToken))
                .ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("The request body is not valid. Details: Input Dicom Tag QueryTagLevel 'Studys' is invalid. It must have value 'Study', 'Series' or 'Instance'.", response.Content.ReadAsStringAsync().Result);
        }

        private void CompareExtendedQueryTagEntries(AddExtendedQueryTag addedTag, GetExtendedQueryTag returnedTag)
        {
            if (addedTag == null || returnedTag == null)
            {
                Assert.True(addedTag == null && returnedTag == null);
                return;
            }

            Assert.True(string.Equals(returnedTag.Path, addedTag.Path, StringComparison.OrdinalIgnoreCase)
                && string.Equals(addedTag.VR, returnedTag.VR, StringComparison.OrdinalIgnoreCase)
                && string.Equals(addedTag.QueryTagLevel, returnedTag.Level.ToString(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
