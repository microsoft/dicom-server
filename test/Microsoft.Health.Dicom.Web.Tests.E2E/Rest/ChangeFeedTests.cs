// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;
using ChangeFeedAction = Microsoft.Health.Dicom.Client.Models.ChangeFeedAction;
using ChangeFeedState = Microsoft.Health.Dicom.Client.Models.ChangeFeedState;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

[CollectionDefinition("Non-Parallel Collection", DisableParallelization = true)]
public class ChangeFeedTests : IAsyncLifetime, IClassFixture<HttpIntegrationTestFixture<Startup>>
{
    private readonly IDicomWebClient _client;
    private readonly DicomInstancesManager _instancesManager;

    public ChangeFeedTests(HttpIntegrationTestFixture<Startup> fixture)
    {
        _client = EnsureArg.IsNotNull(fixture?.GetDicomWebClient(), nameof(fixture));
        _instancesManager = new DicomInstancesManager(_client);
    }

    public Task InitializeAsync()
        => Task.CompletedTask;

    public async Task DisposeAsync()
        => await _instancesManager.DisposeAsync();

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

        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await _client.GetChangeFeed(ToQueryString(offset: initialSequence - 1)))
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
    [Trait("Category", "bvt")]
    public async Task GivenChanges_WhenQueryingWithWindow_ThenScopeResults()
    {
        ChangeFeedEntry[] testChanges;

        // Insert data over time
        DateTimeOffset start = DateTimeOffset.UtcNow;
        InstanceIdentifier instance1 = await CreateFileAsync();
        await Task.Delay(1000);
        InstanceIdentifier instance2 = await CreateFileAsync();
        InstanceIdentifier instance3 = await CreateFileAsync();

        // Get all creation events
        var testRange = new TimeRange(start.AddMilliseconds(-1), DateTimeOffset.UtcNow.AddMilliseconds(1));
        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await _client.GetChangeFeed(ToQueryString(testRange.Start, testRange.End, 0, 10)))
        {
            testChanges = await response.ToArrayAsync();

            Assert.Equal(3, testChanges.Length);
            Assert.Equal(instance1.SopInstanceUid, testChanges[0].SopInstanceUid);
            Assert.Equal(instance2.SopInstanceUid, testChanges[1].SopInstanceUid);
            Assert.Equal(instance3.SopInstanceUid, testChanges[2].SopInstanceUid);
        }

        // Fetch changes outside of the range
        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await _client.GetChangeFeed(ToQueryString(endTime: testChanges[0].Timestamp, offset: 0, limit: 100)))
        {
            ChangeFeedEntry[] changes = await response.ToArrayAsync();
            Assert.DoesNotContain(changes, x => testChanges.Any(y => y.Sequence == x.Sequence));
        }

        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await _client.GetChangeFeed(ToQueryString(startTime: testChanges[1].Timestamp, offset: 2, limit: 100)))
        {
            Assert.Empty(await response.ToArrayAsync());
        }

        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await _client.GetChangeFeed(ToQueryString(startTime: testChanges[2].Timestamp.AddMilliseconds(1), offset: 0, limit: 100)))
        {
            Assert.Empty(await response.ToArrayAsync());
        }

        // Fetch changes limited to window
        await ValidateSubsetAsync(testRange, testChanges[0], testChanges[1], testChanges[2]);
        await ValidateSubsetAsync(new TimeRange(testChanges[0].Timestamp, testChanges[2].Timestamp), testChanges[0], testChanges[1]);
    }

    [Fact]
    public async Task GivenADeletedAndReplacedInstance_WhenRetrievingChangeFeed_ThenTheExpectedInstanceAreReturned()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var seriesInstanceUid = TestUidGenerator.Generate();
        var sopInstanceUid = TestUidGenerator.Generate();

        await CreateFileAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

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

        await CreateFileAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

        using (DicomWebResponse<ChangeFeedEntry> response = await _client.GetChangeFeedLatest())
        {
            await ValidateFeedAsync(response, ChangeFeedAction.Create, ChangeFeedState.Current);
        }

        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await _client.GetChangeFeed(ToQueryString(offset: initialSequence - 1)))
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

        using (DicomWebResponse<ChangeFeedEntry> response = await _client.GetChangeFeedLatest("?includemetadata=false"))
        {
            ChangeFeedEntry changeFeedEntry = await response.GetValueAsync();

            initialSequence = changeFeedEntry.Sequence;

            Assert.Equal(id.StudyInstanceUid, changeFeedEntry.StudyInstanceUid);
            Assert.Equal(id.SeriesInstanceUid, changeFeedEntry.SeriesInstanceUid);
            Assert.Equal(id.SopInstanceUid, changeFeedEntry.SopInstanceUid);
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

    [Theory]
    [InlineData("foo", "2023-05-03T10:58:00Z")]
    [InlineData("2023-05-03T10:58:00Z", "bar")]
    [InlineData("2023-05-03T10:58:00", "2023-05-03T11:00:00Z")]
    [InlineData("2023-05-03T10:58:00Z", "2023-05-03T11:00:00")]
    [InlineData("2023-05-03T10:58:00Z", "2023-05-03T10:58:00Z")]
    [InlineData("2023-05-03T10:59:00Z", "2023-05-03T10:58:00Z")]
    public async Task GivenAnInvalidTimestamps_WhenRetrievingChangeFeed_ThenBadRequestReturned(string startTime, string endTime)
    {
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.GetChangeFeed($"?startTime={startTime}&endTime={endTime}"));
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task GivenAnInvalidParameter_WhenRetrievingChangeFeedLatest_ThenBadRequestReturned()
    {
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
            () => _client.GetChangeFeedLatest("?includeMetadata=asdf"));
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    private async Task<InstanceIdentifier> CreateFileAsync(string studyInstanceUid = null, string seriesInstanceUid = null, string sopInstanceUid = null)
    {
        studyInstanceUid ??= TestUidGenerator.Generate();
        seriesInstanceUid ??= TestUidGenerator.Generate();
        sopInstanceUid ??= TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

        using DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(new[] { dicomFile }, studyInstanceUid);
        DicomDataset dataset = await response.GetValueAsync();

        return new InstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
    }

    private async Task ValidateSubsetAsync(TimeRange range, params ChangeFeedEntry[] expected)
    {
        for (int i = 0; i < expected.Length; i++)
        {
            using DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await _client.GetChangeFeed(ToQueryString(range.Start, range.End, i, 1));
            ChangeFeedEntry[] changes = await response.ToArrayAsync();

            Assert.Single(changes);
            Assert.Equal(expected[i].Sequence, changes.Single().Sequence);
        }

        using DicomWebAsyncEnumerableResponse<ChangeFeedEntry> emptyResponse = await _client.GetChangeFeed(ToQueryString(range.Start, range.End, expected.Length, 1));
        Assert.Empty(await emptyResponse.ToArrayAsync());
    }

    private static string ToQueryString(
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        long? offset = null,
        int? limit = null,
        bool? includeMetadata = null)
    {
        var builder = new StringBuilder("?");

        if (startTime.HasValue)
            builder.Append(CultureInfo.InvariantCulture, $"{nameof(startTime)}={HttpUtility.UrlEncode(startTime.GetValueOrDefault().ToString("O", CultureInfo.InvariantCulture))}");

        if (endTime.HasValue)
        {
            if (builder.Length > 1)
                builder.Append('&');

            builder.Append(CultureInfo.InvariantCulture, $"{nameof(endTime)}={HttpUtility.UrlEncode(endTime.GetValueOrDefault().ToString("O", CultureInfo.InvariantCulture))}");
        }

        if (offset.HasValue)
        {
            if (builder.Length > 1)
                builder.Append('&');

            builder.Append(CultureInfo.InvariantCulture, $"{nameof(offset)}={offset.GetValueOrDefault()}");
        }

        if (limit.HasValue)
        {
            if (builder.Length > 1)
                builder.Append('&');

            builder.Append(CultureInfo.InvariantCulture, $"{nameof(limit)}={limit.GetValueOrDefault()}");
        }

        if (includeMetadata.HasValue)
        {
            if (builder.Length > 1)
                builder.Append('&');

            builder.Append(CultureInfo.InvariantCulture, $"{nameof(includeMetadata)}={includeMetadata.GetValueOrDefault()}");
        }

        return builder.ToString();
    }
}
