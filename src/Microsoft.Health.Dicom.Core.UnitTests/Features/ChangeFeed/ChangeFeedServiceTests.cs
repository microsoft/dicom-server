// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
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
        _changeFeedService = new ChangeFeedService(_changeFeedStore, _metadataStore);
    }

    [Fact]
    public async Task GivenChangeFeed_WhenFetchingWithoutMetadata_ThenOnlyCheckStore()
    {
        const int offset = 10;
        const int limit = 50;
        var range = new DateTimeOffsetRange(DateTimeOffset.UtcNow, DateTime.UtcNow.AddHours(1));
        var expected = new List<ChangeFeedEntry>();

        using var tokenSource = new CancellationTokenSource();

        _changeFeedStore.GetChangeFeedAsync(range, offset, limit, tokenSource.Token).Returns(expected);

        IReadOnlyCollection<ChangeFeedEntry> actual = await _changeFeedService.GetChangeFeedAsync(range, offset, limit, false, tokenSource.Token);

        await _changeFeedStore.Received(1).GetChangeFeedAsync(range, offset, limit, tokenSource.Token);
        await _metadataStore.DidNotReceiveWithAnyArgs().GetInstanceMetadataAsync(default, default);

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task GivenChangeFeed_WhenFetchingWithMetadata_ThenFetchMetadataToo()
    {
        const int offset = 10;
        const int limit = 50;
        var range = new DateTimeOffsetRange(DateTimeOffset.UtcNow, DateTime.UtcNow.AddHours(1));
        var expected = new List<ChangeFeedEntry>
        {
            new ChangeFeedEntry(1, DateTime.Now, ChangeFeedAction.Create, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1, 1, ChangeFeedState.Current),
            new ChangeFeedEntry(2, DateTime.Now, ChangeFeedAction.Create, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 2, 2, ChangeFeedState.Deleted),
            new ChangeFeedEntry(3, DateTime.Now, ChangeFeedAction.Create, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 3, 4, ChangeFeedState.Replaced),
        };
        var expectedDataset1 = new DicomDataset();
        var expectedDataset3 = new DicomDataset();

        using var tokenSource = new CancellationTokenSource();

        _changeFeedStore.GetChangeFeedAsync(range, offset, limit, tokenSource.Token).Returns(expected);
        _metadataStore.GetInstanceMetadataAsync(1, tokenSource.Token).Returns(expectedDataset1);
        _metadataStore.GetInstanceMetadataAsync(3, tokenSource.Token).Returns(expectedDataset3);

        IReadOnlyCollection<ChangeFeedEntry> actual = await _changeFeedService.GetChangeFeedAsync(range, offset, limit, false, tokenSource.Token);

        await _changeFeedStore.Received(1).GetChangeFeedAsync(range, offset, limit, tokenSource.Token);
        await _metadataStore.Received(1).GetInstanceMetadataAsync(1, tokenSource.Token);
        await _metadataStore.DidNotReceive().GetInstanceMetadataAsync(2, tokenSource.Token);
        await _metadataStore.Received(1).GetInstanceMetadataAsync(3, tokenSource.Token);

        Assert.Same(expected, actual);
        Assert.Same(expectedDataset1, expected[0].Metadata);
        Assert.Null(expected[1].Metadata);
        Assert.Same(expectedDataset3, expected[2].Metadata);
    }

    [Fact]
    public async Task GivenChangeFeed_WhenFetchingLatestWithoutMetadata_ThenOnlyCheckStore()
    {
        var expected = new ChangeFeedEntry(1, DateTime.Now, ChangeFeedAction.Create, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1, 1, ChangeFeedState.Current);
        using var tokenSource = new CancellationTokenSource();

        _changeFeedStore.GetChangeFeedLatestAsync(tokenSource.Token).Returns(expected);

        ChangeFeedEntry actual = await _changeFeedService.GetChangeFeedLatestAsync(false, tokenSource.Token);

        await _changeFeedStore.Received(1).GetChangeFeedLatestAsync(tokenSource.Token);
        await _metadataStore.DidNotReceiveWithAnyArgs().GetInstanceMetadataAsync(default, default);

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task GivenChangeFeed_WhenFetchingLatestDeletedWithMetadata_ThenSkipMetadata()
    {
        var expected = new ChangeFeedEntry(1, DateTime.Now, ChangeFeedAction.Create, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1, null, ChangeFeedState.Deleted);
        using var tokenSource = new CancellationTokenSource();

        _changeFeedStore.GetChangeFeedLatestAsync(tokenSource.Token).Returns(expected);

        ChangeFeedEntry actual = await _changeFeedService.GetChangeFeedLatestAsync(true, tokenSource.Token);

        await _changeFeedStore.Received(1).GetChangeFeedLatestAsync(tokenSource.Token);
        await _metadataStore.DidNotReceiveWithAnyArgs().GetInstanceMetadataAsync(default, default);

        Assert.Same(expected, actual);
    }
}
