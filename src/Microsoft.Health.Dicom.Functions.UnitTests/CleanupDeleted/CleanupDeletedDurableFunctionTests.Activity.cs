// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.BlobMigration.Models;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.CleanupDeleted;

public partial class CleanupDeletedDurableFunctionTests
{
    [Fact]
    public async Task GivenTimStamp_WhenGettingDeletedChangeFeedInstanceBatches_ThenShouldInvokeCorrectMethod()
    {
        const int batchSize = 100;
        DateTime timeStamp = DateTime.UtcNow;

        IReadOnlyCollection<ChangeFeedEntry> expected = new List<ChangeFeedEntry> {
            new ChangeFeedEntry(1, DateTime.Now, ChangeFeedAction.Delete, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1, 1, ChangeFeedState.Current),
            new ChangeFeedEntry(2, DateTime.Now, ChangeFeedAction.Delete, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 2, 2, ChangeFeedState.Current),
        };

        _changeFeedStore
            .GetDeletedChangeFeedByWatermarkOrTimeStampAsync(batchSize, timeStamp, null)
            .Returns(expected);

        IReadOnlyCollection<ChangeFeedEntry> actual = await _function.GetDeletedChangeFeedInstanceBatchesAsync(
            new CleanupDeletedBatchArguments
            {
                BatchSize = batchSize,
                FilterTimeStamp = timeStamp
            },
            NullLogger.Instance);

        Assert.Same(expected, actual);

        await _changeFeedStore
            .Received(1)
            .GetDeletedChangeFeedByWatermarkOrTimeStampAsync(batchSize, timeStamp, null);
    }

    [Fact]
    public async Task GivenWatermark_WhenGettingDeletedChangeFeedInstanceBatches_ThenShouldInvokeCorrectMethod()
    {
        const int batchSize = 100;
        WatermarkRange watermarkRange = new WatermarkRange(1, 2);

        IReadOnlyCollection<ChangeFeedEntry> expected = new List<ChangeFeedEntry> {
            new ChangeFeedEntry(1, DateTime.Now, ChangeFeedAction.Delete, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1, 1, ChangeFeedState.Current),
            new ChangeFeedEntry(2, DateTime.Now, ChangeFeedAction.Delete, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 2, 2, ChangeFeedState.Current),
        };

        _changeFeedStore
            .GetDeletedChangeFeedByWatermarkOrTimeStampAsync(batchSize, null, new WatermarkRange(1, 2))
            .Returns(expected);

        IReadOnlyCollection<ChangeFeedEntry> actual = await _function.GetDeletedChangeFeedInstanceBatchesAsync(
            new CleanupDeletedBatchArguments
            {
                BatchSize = batchSize,
                BatchRange = watermarkRange
            },
            NullLogger.Instance);

        Assert.Same(expected, actual);

        await _changeFeedStore
            .Received(1)
            .GetDeletedChangeFeedByWatermarkOrTimeStampAsync(batchSize, null, watermarkRange);
    }

    [Fact]
    public async Task GivenTimeStamp_WhenGettingWatermark_ThenShouldInvokeCorrectMethod()
    {
        DateTime timeStamp = DateTime.UtcNow;
        long maxWatermark = 50;

        _changeFeedStore
            .GetMaxDeletedChangeFeedWatermarkAsync(timeStamp)
            .Returns(maxWatermark);

        long actual = await _function.GetMaxDeletedChangeFeedWatermarkAsync(
            new CleanupDeletedBatchArguments
            {
                FilterTimeStamp = timeStamp,
            },
            NullLogger.Instance);

        Assert.Equal(maxWatermark, actual);

        await _changeFeedStore
            .Received(1)
            .GetMaxDeletedChangeFeedWatermarkAsync(timeStamp);
    }

    [Fact]
    public async Task GivenIdentifiers_WhenDeleting_ThenShouldDeleteEachInstance()
    {
        var identifiers = new List<VersionedInstanceIdentifier>
        {
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 2)
        };

        await _function.CleanupDeletedBatchAsync(identifiers, NullLogger.Instance);


        foreach (VersionedInstanceIdentifier identifier in identifiers)
        {
            await _blobMigrationService.Received(1).DeleteInstanceAsync(identifier, true, Arg.Any<CancellationToken>());
        }
    }
}
