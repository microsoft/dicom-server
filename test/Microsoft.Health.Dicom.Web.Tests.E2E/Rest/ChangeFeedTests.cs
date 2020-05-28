// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class ChangeFeedTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly DicomWebClient _client;

        public ChangeFeedTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task GivenASetOfDicomInstances_WhenRetrievingChangeFeed_ThenTheExpectedInstanceAreReturned()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUids = Enumerable.Range(1, 10).Select(x => TestUidGenerator.Generate()).ToArray();
            long initialSequence = -1;

            for (int i = 0; i < 10; i++)
            {
                await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUids[i]);
                if (initialSequence == -1)
                {
                    var result = await _client.GetChangeFeedLatest();
                    initialSequence = result.Value.Sequence;
                    Assert.Equal(studyInstanceUid, result.Value.StudyInstanceUid);
                    Assert.Equal(seriesInstanceUid, result.Value.SeriesInstanceUid);
                    Assert.Equal(sopInstanceUids[i], result.Value.SopInstanceUid);
                    Assert.Equal(ChangeFeedAction.Create, result.Value.Action);
                    Assert.Equal(ChangeFeedState.Current, result.Value.State);
                }
            }

            var changeFeedResults = await _client.GetChangeFeed();
            Assert.Equal(10, changeFeedResults.Value.Count);

            changeFeedResults = await _client.GetChangeFeed($"?offset={initialSequence - 1}");
            Assert.Equal(10, changeFeedResults.Value.Count);
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(studyInstanceUid, changeFeedResults.Value[i].StudyInstanceUid);
                Assert.Equal(seriesInstanceUid, changeFeedResults.Value[i].SeriesInstanceUid);
                Assert.Equal(sopInstanceUids[i], changeFeedResults.Value[i].SopInstanceUid);
                Assert.NotNull(changeFeedResults.Value[i].Metadata);
            }
        }

        [Fact]
        public async Task GivenADeletedAndReplacedInstance_WhenRetrievingChangeFeed_ThenTheExpectedInstanceAreReturned()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            var latestResult = await _client.GetChangeFeedLatest();
            long initialSequence = latestResult.Value.Sequence;
            ValidateFeed(ChangeFeedAction.Create, ChangeFeedState.Current);

            await _client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            latestResult = await _client.GetChangeFeedLatest();
            ValidateFeed(ChangeFeedAction.Delete, ChangeFeedState.Deleted);

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            latestResult = await _client.GetChangeFeedLatest();
            ValidateFeed(ChangeFeedAction.Create, ChangeFeedState.Current);

            var changeFeedResults = await _client.GetChangeFeed($"?offset={initialSequence - 1}");
            Assert.Equal(3, changeFeedResults.Value.Count);

            Assert.Equal(ChangeFeedState.Replaced, changeFeedResults.Value[0].State);
            Assert.Equal(ChangeFeedState.Replaced, changeFeedResults.Value[1].State);
            Assert.Equal(ChangeFeedState.Current, changeFeedResults.Value[2].State);

            void ValidateFeed(ChangeFeedAction action, ChangeFeedState state)
            {
                Assert.Equal(studyInstanceUid, latestResult.Value.StudyInstanceUid);
                Assert.Equal(seriesInstanceUid, latestResult.Value.SeriesInstanceUid);
                Assert.Equal(sopInstanceUid, latestResult.Value.SopInstanceUid);
                Assert.Equal(action, latestResult.Value.Action);
                Assert.Equal(state, latestResult.Value.State);

                if (state == ChangeFeedState.Deleted)
                {
                    Assert.Null(latestResult.Value.Metadata);
                }
                else
                {
                    Assert.NotNull(latestResult.Value.Metadata);
                }
            }
        }

        [Fact]
        public async Task GivenAnInstance_WhenRetrievingChangeFeedWithoutMetadata_ThenMetadataIsNotReturned()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            var latestResult = await _client.GetChangeFeedLatest("?includemetadata=false");
            long initialSequence = latestResult.Value.Sequence;
            Assert.Equal(studyInstanceUid, latestResult.Value.StudyInstanceUid);
            Assert.Equal(seriesInstanceUid, latestResult.Value.SeriesInstanceUid);
            Assert.Equal(sopInstanceUid, latestResult.Value.SopInstanceUid);
            Assert.Null(latestResult.Value.Metadata);

            var changeFeedResults = await _client.GetChangeFeed($"?offset={initialSequence - 1}&includemetadata=false");
            Assert.Equal(1, changeFeedResults.Value.Count);
            Assert.Null(changeFeedResults.Value[0].Metadata);
            Assert.Equal(ChangeFeedState.Current, changeFeedResults.Value[0].State);
        }

        [Theory]
        [InlineData("-1", "0", "true")]
        [InlineData("0", "0", "true")]
        [InlineData("101", "0", "true")]
        [InlineData("blah", "0", "true")]
        [InlineData("1", "-1", "true")]
        [InlineData("1", "92233720368547758070", "true")]
        [InlineData("1", "blah", "true")]
        [InlineData("1", "0", "0")]
        [InlineData("1", "0", "true-ish")]
        [InlineData("1", "0", "boolean")]
        public async Task GivenAnInvalidParameter_WhenRetrievingChangeFeed_ThenBadRequestReturned(string limit, string offset, string includeMetadata)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => _client.GetChangeFeed($"?limit={limit}&offset={offset}&includeMetadata={includeMetadata}"));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        [Fact]
        public async Task GivenAnInvalidParameter_WhenRetrievingChangeFeedLatest_ThenBadRequestReturned()
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => _client.GetChangeFeedLatest("?includeMetadata=asdf"));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        private async Task CreateFile(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            await _client.StoreAsync(new[] { dicomFile1 }, studyInstanceUid);
        }
    }
}
