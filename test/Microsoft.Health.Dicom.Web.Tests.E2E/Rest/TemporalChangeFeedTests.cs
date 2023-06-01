// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Xunit;
using PartitionEntry = Microsoft.Health.Dicom.Core.Features.Partition.PartitionEntry;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

// TODO: Remove trait and combine tests with other change feed tests once V2 is live
[Trait("Category", "leniency")]
[Collection("Change Feed Collection")]
public class TemporalChangeFeedTests : BaseChangeFeedTests, IClassFixture<FeatureEnabledTestFixture<Startup>>
{
    public TemporalChangeFeedTests(FeatureEnabledTestFixture<Startup> fixture)
        : base(fixture.GetDicomWebClient("v2"))
    {
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
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => Client.GetChangeFeed($"?startTime={startTime}&endTime={endTime}"));
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task GivenChanges_WhenQueryingWithWindow_ThenScopeResults()
    {
        ChangeFeedEntry[] testChanges;

        // Insert data over time
        DateTimeOffset start = DateTimeOffset.UtcNow;
        InstanceIdentifier instance1 = await CreateFileAsync(partitionName: PartitionEntry.Default.PartitionName);
        await Task.Delay(1000);
        InstanceIdentifier instance2 = await CreateFileAsync(partitionName: PartitionEntry.Default.PartitionName);
        InstanceIdentifier instance3 = await CreateFileAsync(partitionName: PartitionEntry.Default.PartitionName);

        // Get all creation events
        var testRange = new TimeRange(start.AddMilliseconds(-1), DateTimeOffset.UtcNow.AddMilliseconds(1));
        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await Client.GetChangeFeed(ToQueryString(testRange.Start, testRange.End, 0, 10)))
        {
            testChanges = await response.ToArrayAsync();

            Assert.Equal(3, testChanges.Length);
            Assert.Equal(instance1.SopInstanceUid, testChanges[0].SopInstanceUid);
            Assert.Equal(instance2.SopInstanceUid, testChanges[1].SopInstanceUid);
            Assert.Equal(instance3.SopInstanceUid, testChanges[2].SopInstanceUid);
        }

        // Fetch changes outside of the range
        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await Client.GetChangeFeed(ToQueryString(endTime: testChanges[0].Timestamp, offset: 0, limit: 100)))
        {
            ChangeFeedEntry[] changes = await response.ToArrayAsync();
            Assert.DoesNotContain(changes, x => testChanges.Any(y => y.Sequence == x.Sequence));
        }

        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await Client.GetChangeFeed(ToQueryString(startTime: testChanges[1].Timestamp, offset: 2, limit: 100)))
        {
            Assert.Empty(await response.ToArrayAsync());
        }

        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await Client.GetChangeFeed(ToQueryString(startTime: testChanges[2].Timestamp.AddMilliseconds(1), offset: 0, limit: 100)))
        {
            Assert.Empty(await response.ToArrayAsync());
        }

        // Fetch changes limited to window
        await ValidateSubsetAsync(testRange, testChanges[0], testChanges[1], testChanges[2]);
        await ValidateSubsetAsync(new TimeRange(testChanges[0].Timestamp, testChanges[2].Timestamp), testChanges[0], testChanges[1]);
    }

    private async Task ValidateSubsetAsync(TimeRange range, params ChangeFeedEntry[] expected)
    {
        for (int i = 0; i < expected.Length; i++)
        {
            using DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await Client.GetChangeFeed(ToQueryString(range.Start, range.End, i, 1));
            ChangeFeedEntry[] changes = await response.ToArrayAsync();

            Assert.Single(changes);
            Assert.Equal(expected[i].Sequence, changes.Single().Sequence);
        }

        using DicomWebAsyncEnumerableResponse<ChangeFeedEntry> emptyResponse = await Client.GetChangeFeed(ToQueryString(range.Start, range.End, expected.Length, 1));
        Assert.Empty(await emptyResponse.ToArrayAsync());
    }
}
