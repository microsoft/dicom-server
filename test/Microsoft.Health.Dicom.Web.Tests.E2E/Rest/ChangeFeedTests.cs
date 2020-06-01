// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.IO;
using Xunit;
using ChangeFeedAction = Microsoft.Health.Dicom.Client.ChangeFeedAction;
using ChangeFeedState = Microsoft.Health.Dicom.Client.ChangeFeedState;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class ChangeFeedTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly TestDicomWebClient _client;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public ChangeFeedTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
            _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
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

        [Fact]
        public async Task GivenANegativeOffset_WhenRetrievingChangeFeed_ThenBadRequestReturned()
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => _client.GetChangeFeed("?offset=-1"));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(101)]
        public async Task GivenAnInvalidLimit_WhenRetrievingChangeFeed_ThenBadRequestReturned(int limit)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => _client.GetChangeFeed($"?limit={limit}"));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        private async Task CreateFile(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            await _client.StoreAsync(new[] { dicomFile1 }, studyInstanceUid);
        }
    }
}
