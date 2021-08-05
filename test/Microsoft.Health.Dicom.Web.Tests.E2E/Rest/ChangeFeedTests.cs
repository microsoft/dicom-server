// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;
using ChangeFeedAction = Microsoft.Health.Dicom.Client.Models.ChangeFeedAction;
using ChangeFeedState = Microsoft.Health.Dicom.Client.Models.ChangeFeedState;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    [CollectionDefinition("Non-Parallel Collection", DisableParallelization = true)]
    public class ChangeFeedTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly IDicomWebClient _client;

        public ChangeFeedTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _client = fixture.Client;
        }

        [Fact]
        [Trait("Category", "bvt")]
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
                    using DicomWebResponse<ChangeFeedEntry> latestResponse = await _client.GetChangeFeedLatest();

                    ChangeFeedEntry result = await latestResponse.GetValueAsync();

                    initialSequence = result.Sequence;
                    Assert.Equal(studyInstanceUid, result.StudyInstanceUid);
                    Assert.Equal(seriesInstanceUid, result.SeriesInstanceUid);
                    Assert.Equal(sopInstanceUids[i], result.SopInstanceUid);
                    Assert.Equal(ChangeFeedAction.Create, result.Action);
                    Assert.Equal(ChangeFeedState.Current, result.State);
                }
            }

            using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await _client.GetChangeFeed())
            {
                Assert.Equal(10, await response.CountAsync());
            }

            using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await _client.GetChangeFeed($"?offset={initialSequence - 1}"))
            {
                ChangeFeedEntry[] changeFeedResults = await response.ToArrayAsync();

                Assert.Equal(10, changeFeedResults.Length);

                for (int i = 0; i < 10; i++)
                {
                    Assert.Equal(studyInstanceUid, changeFeedResults[i].StudyInstanceUid);
                    Assert.Equal(seriesInstanceUid, changeFeedResults[i].SeriesInstanceUid);
                    Assert.Equal(sopInstanceUids[i], changeFeedResults[i].SopInstanceUid);
                    Assert.NotNull(changeFeedResults[i].Metadata);
                }
            }
        }

        [Fact]
        public async Task GivenADeletedAndReplacedInstance_WhenRetrievingChangeFeed_ThenTheExpectedInstanceAreReturned()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            long initialSequence;

            using (DicomWebResponse<ChangeFeedEntry> response = await _client.GetChangeFeedLatest())
            {
                initialSequence = (await ValidateFeedAsync(response, ChangeFeedAction.Create, ChangeFeedState.Current)).Sequence;
            }

            await _client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            using (DicomWebResponse<ChangeFeedEntry> response = await _client.GetChangeFeedLatest())
            {
                await ValidateFeedAsync(response, ChangeFeedAction.Delete, ChangeFeedState.Deleted);
            }

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            using (DicomWebResponse<ChangeFeedEntry> response = await _client.GetChangeFeedLatest())
            {
                await ValidateFeedAsync(response, ChangeFeedAction.Create, ChangeFeedState.Current);
            }

            using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await _client.GetChangeFeed($"?offset={initialSequence - 1}"))
            {
                ChangeFeedEntry[] changeFeedResults = await response.ToArrayAsync();

                Assert.Equal(3, changeFeedResults.Length);

                Assert.Equal(ChangeFeedState.Replaced, changeFeedResults[0].State);
                Assert.Equal(ChangeFeedState.Replaced, changeFeedResults[1].State);
                Assert.Equal(ChangeFeedState.Current, changeFeedResults[2].State);
            }

            async Task<ChangeFeedEntry> ValidateFeedAsync(DicomWebResponse<ChangeFeedEntry> response, ChangeFeedAction action, ChangeFeedState state)
            {
                ChangeFeedEntry changeFeedEntry = await response.GetValueAsync();

                Assert.Equal(studyInstanceUid, changeFeedEntry.StudyInstanceUid);
                Assert.Equal(seriesInstanceUid, changeFeedEntry.SeriesInstanceUid);
                Assert.Equal(sopInstanceUid, changeFeedEntry.SopInstanceUid);
                Assert.Equal(action, changeFeedEntry.Action);
                Assert.Equal(state, changeFeedEntry.State);

                if (state == ChangeFeedState.Deleted)
                {
                    Assert.Null(changeFeedEntry.Metadata);
                }
                else
                {
                    Assert.NotNull(changeFeedEntry.Metadata);
                }

                return changeFeedEntry;
            }
        }

        [Fact]
        public async Task GivenAnInstance_WhenRetrievingChangeFeedWithoutMetadata_ThenMetadataIsNotReturned()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            long initialSequence;

            using (DicomWebResponse<ChangeFeedEntry> response = await _client.GetChangeFeedLatest("?includemetadata=false"))
            {
                ChangeFeedEntry changeFeedEntry = await response.GetValueAsync();

                initialSequence = changeFeedEntry.Sequence;

                Assert.Equal(studyInstanceUid, changeFeedEntry.StudyInstanceUid);
                Assert.Equal(seriesInstanceUid, changeFeedEntry.SeriesInstanceUid);
                Assert.Equal(sopInstanceUid, changeFeedEntry.SopInstanceUid);
                Assert.Null(changeFeedEntry.Metadata);
            }

            using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await _client.GetChangeFeed($"?offset={initialSequence - 1}&includemetadata=false"))
            {
                ChangeFeedEntry[] changeFeedResults = await response.ToArrayAsync();

                Assert.Single(changeFeedResults);
                Assert.Null(changeFeedResults[0].Metadata);
                Assert.Equal(ChangeFeedState.Current, changeFeedResults[0].State);
            }
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
