// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

[CollectionDefinition("ChangeFeedTests Non-Parallel Collection", DisableParallelization = true)]
public class ChangeFeedTests : BaseChangeFeedTests, IClassFixture<HttpIntegrationTestFixture<Startup>>
{
    public ChangeFeedTests(HttpIntegrationTestFixture<Startup> fixture)
        : base(fixture.GetDicomWebClient())
    {
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
            await CreateFileAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUids[i]);

            if (initialSequence == -1)
            {
                using DicomWebResponse<ChangeFeedEntry> latestResponse = await Client.GetChangeFeedLatest();

                ChangeFeedEntry result = await latestResponse.GetValueAsync();

                initialSequence = result.Sequence;
                Assert.Equal(studyInstanceUid, result.StudyInstanceUid);
                Assert.Equal(seriesInstanceUid, result.SeriesInstanceUid);
                Assert.Equal(sopInstanceUids[i], result.SopInstanceUid);
                Assert.Equal(ChangeFeedAction.Create, result.Action);
                Assert.Equal(ChangeFeedState.Current, result.State);
            }
        }

        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await Client.GetChangeFeed())
        {
            Assert.Equal(10, await response.CountAsync());
        }

        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await Client.GetChangeFeed(ToQueryString(offset: initialSequence - 1)))
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

        await CreateFileAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

        long initialSequence;

        using (DicomWebResponse<ChangeFeedEntry> response = await Client.GetChangeFeedLatest())
        {
            initialSequence = (await ValidateFeedAsync(response, ChangeFeedAction.Create, ChangeFeedState.Current)).Sequence;
        }

        await Client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

        using (DicomWebResponse<ChangeFeedEntry> response = await Client.GetChangeFeedLatest())
        {
            await ValidateFeedAsync(response, ChangeFeedAction.Delete, ChangeFeedState.Deleted);
        }

        await CreateFileAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

        using (DicomWebResponse<ChangeFeedEntry> response = await Client.GetChangeFeedLatest())
        {
            await ValidateFeedAsync(response, ChangeFeedAction.Create, ChangeFeedState.Current);
        }

        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await Client.GetChangeFeed(ToQueryString(offset: initialSequence - 1)))
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
        long initialSequence;
        InstanceIdentifier id = await CreateFileAsync();

        using (DicomWebResponse<ChangeFeedEntry> response = await Client.GetChangeFeedLatest(ToQueryString(includeMetadata: false)))
        {
            ChangeFeedEntry changeFeedEntry = await response.GetValueAsync();

            initialSequence = changeFeedEntry.Sequence;

            Assert.Equal(id.StudyInstanceUid, changeFeedEntry.StudyInstanceUid);
            Assert.Equal(id.SeriesInstanceUid, changeFeedEntry.SeriesInstanceUid);
            Assert.Equal(id.SopInstanceUid, changeFeedEntry.SopInstanceUid);
            Assert.Null(changeFeedEntry.Metadata);
        }

        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await Client.GetChangeFeed(ToQueryString(offset: initialSequence - 1, includeMetadata: false)))
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
            () => Client.GetChangeFeed($"?limit={limit}&offset={offset}&includeMetadata={includeMetadata}"));
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task GivenAnInvalidParameter_WhenRetrievingChangeFeedLatest_ThenBadRequestReturned()
    {
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
            () => Client.GetChangeFeedLatest("?includeMetadata=asdf"));
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }
}
