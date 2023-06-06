// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed;

public class ChangeFeedServiceTests
{
    private readonly IChangeFeedStore _changeFeedStore = Substitute.For<IChangeFeedStore>();
    private readonly IMetadataStore _metadataStore = Substitute.For<IMetadataStore>();
    private readonly ChangeFeedService _changeFeedService;

    public ChangeFeedServiceTests()
    {
        // For the test, we'll use unbounded parallelism with -1
        _changeFeedService = new ChangeFeedService(
            _changeFeedStore,
            _metadataStore,
            Options.Create(new RetrieveConfiguration { MaxDegreeOfParallelism = -1 }));
    }

    [Fact]
    public async Task GivenChangeFeed_WhenFetchingWithoutMetadata_ThenOnlyCheckStore()
    {
        const int offset = 10;
        const int limit = 50;
        const ChangeFeedOrder order = ChangeFeedOrder.Sequence;
        TimeRange range = TimeRange.MaxValue;
        var expected = new List<ChangeFeedEntry>
        {
            new ChangeFeedEntry(1, DateTime.Now, ChangeFeedAction.Create, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 101, 101, ChangeFeedState.Current),
        };

        using var tokenSource = new CancellationTokenSource();

        _changeFeedStore.GetChangeFeedAsync(range, offset, limit, order, tokenSource.Token).Returns(expected);

        IReadOnlyList<ChangeFeedEntry> actual = await _changeFeedService.GetChangeFeedAsync(range, offset, limit, order, false, tokenSource.Token);

        await _changeFeedStore.Received(1).GetChangeFeedAsync(range, offset, limit, order, tokenSource.Token);
        await _metadataStore.DidNotReceiveWithAnyArgs().GetInstanceMetadataAsync(default, default);

        Assert.Same(expected, actual);
        Assert.True(actual.All(x => !x.IncludeMetadata));
        Assert.True(actual.All(x => x.Metadata == null));
    }

    [Fact]
    public async Task GivenChangeFeed_WhenFetchingWithMetadata_ThenFetchMetadataToo()
    {
        const int offset = 10;
        const int limit = 50;
        const ChangeFeedOrder order = ChangeFeedOrder.Time;
        var range = new TimeRange(DateTimeOffset.UtcNow, DateTime.UtcNow.AddHours(1));
        var expected = new List<ChangeFeedEntry>
        {
            new ChangeFeedEntry(1, DateTime.Now, ChangeFeedAction.Create, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 101, 101, ChangeFeedState.Current),
            new ChangeFeedEntry(2, DateTime.Now, ChangeFeedAction.Create, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 102, null, ChangeFeedState.Deleted),
            new ChangeFeedEntry(3, DateTime.Now, ChangeFeedAction.Create, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 103, 104, ChangeFeedState.Replaced),
        };
        var expectedDataset1 = new DicomDataset();
        var expectedDataset3 = new DicomDataset();

        using var tokenSource = new CancellationTokenSource();

        // Note: Parallel.ForEachAsync uses its own CancellationToken
        _changeFeedStore.GetChangeFeedAsync(range, offset, limit, order, tokenSource.Token).Returns(expected);
        _metadataStore.GetInstanceMetadataAsync(101, Arg.Any<CancellationToken>()).Returns(expectedDataset1);
        _metadataStore.GetInstanceMetadataAsync(104, Arg.Any<CancellationToken>()).Returns(expectedDataset3);

        IReadOnlyList<ChangeFeedEntry> actual = await _changeFeedService.GetChangeFeedAsync(range, offset, limit, order, true, tokenSource.Token);

        await _changeFeedStore.Received(1).GetChangeFeedAsync(range, offset, limit, order, tokenSource.Token);
        await _metadataStore.Received(1).GetInstanceMetadataAsync(101, Arg.Any<CancellationToken>());
        await _metadataStore.DidNotReceive().GetInstanceMetadataAsync(102, tokenSource.Token);
        await _metadataStore.Received(1).GetInstanceMetadataAsync(104, Arg.Any<CancellationToken>());

        Assert.Same(expected, actual);
        Assert.True(actual[0].IncludeMetadata);
        Assert.Same(expectedDataset1, actual[0].Metadata);
        Assert.False(actual[1].IncludeMetadata);
        Assert.Null(expected[1].Metadata);
        Assert.True(actual[2].IncludeMetadata);
        Assert.Same(expectedDataset3, actual[2].Metadata);
    }

    [Fact]
    public async Task GivenChangeFeed_WhenFetchingLatestWithoutMetadata_ThenOnlyCheckStore()
    {
        const ChangeFeedOrder order = ChangeFeedOrder.Time;
        var expected = new ChangeFeedEntry(1, DateTime.Now, ChangeFeedAction.Create, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 101, 101, ChangeFeedState.Current);
        using var tokenSource = new CancellationTokenSource();

        _changeFeedStore.GetChangeFeedLatestAsync(order, tokenSource.Token).Returns(expected);

        ChangeFeedEntry actual = await _changeFeedService.GetChangeFeedLatestAsync(order, false, tokenSource.Token);

        await _changeFeedStore.Received(1).GetChangeFeedLatestAsync(order, tokenSource.Token);
        await _metadataStore.DidNotReceiveWithAnyArgs().GetInstanceMetadataAsync(default, default);

        Assert.Same(expected, actual);
        Assert.False(actual.IncludeMetadata);
        Assert.Null(actual.Metadata);
    }

    [Fact]
    public async Task GivenChangeFeed_WhenFetchingLatestDeletedWithMetadata_ThenOnlyCheckStore()
    {
        const ChangeFeedOrder order = ChangeFeedOrder.Sequence;
        var expected = new ChangeFeedEntry(1, DateTime.Now, ChangeFeedAction.Create, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 101, null, ChangeFeedState.Deleted);
        using var tokenSource = new CancellationTokenSource();

        _changeFeedStore.GetChangeFeedLatestAsync(order, tokenSource.Token).Returns(expected);

        ChangeFeedEntry actual = await _changeFeedService.GetChangeFeedLatestAsync(order, true, tokenSource.Token);

        await _changeFeedStore.Received(1).GetChangeFeedLatestAsync(order, tokenSource.Token);
        await _metadataStore.DidNotReceiveWithAnyArgs().GetInstanceMetadataAsync(default, default);

        Assert.Same(expected, actual);
        Assert.False(actual.IncludeMetadata);
        Assert.Null(actual.Metadata);
    }
}
