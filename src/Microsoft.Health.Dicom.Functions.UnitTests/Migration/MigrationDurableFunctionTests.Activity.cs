// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Functions.Migration.Models;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Migration;

public partial class MigrationDurableFunctionTests
{
    [Fact]
    public async Task GivenNoWatermark_WhenGettingInstanceBatches_ThenShouldInvokeCorrectMethod()
    {
        const int batchSize = 100;
        const int maxParallelBatches = 3;
        var now = DateTime.UtcNow;
        var startTimeStamp = now;
        var endTimeStamp = now.AddDays(1);

        IReadOnlyList<WatermarkRange> expected = new List<WatermarkRange> { new WatermarkRange(12345, 678910) };
        _instanceStore
            .GetInstanceBatchesByTimeStampAsync(batchSize, maxParallelBatches, IndexStatus.Created, startTimeStamp, endTimeStamp, null, CancellationToken.None)
            .Returns(expected);

        IReadOnlyList<WatermarkRange> actual = await _migrationDurableFunction.GetInstanceBatchesByTimeStampAsync(
            new MigrationBatchCreationArguments(null, batchSize, maxParallelBatches, startTimeStamp, endTimeStamp),
            NullLogger.Instance);

        Assert.Same(expected, actual);
        await _instanceStore
            .Received(1)
            .GetInstanceBatchesByTimeStampAsync(batchSize, maxParallelBatches, IndexStatus.Created, startTimeStamp, endTimeStamp, null, CancellationToken.None);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public async Task GivenWatermark_WhenGettingInstanceBatches_ThenShouldInvokeCorrectMethod(long max)
    {
        const int batchSize = 100;
        const int maxParallelBatches = 3;
        var now = DateTime.UtcNow;
        var startTimeStamp = now;
        var endTimeStamp = now.AddDays(1);

        IReadOnlyList<WatermarkRange> expected = new List<WatermarkRange> { new WatermarkRange(1, 2) }; // watermarks don't matter
        _instanceStore
            .GetInstanceBatchesByTimeStampAsync(batchSize, maxParallelBatches, IndexStatus.Created, startTimeStamp, endTimeStamp, max, CancellationToken.None)
            .Returns(expected);

        IReadOnlyList<WatermarkRange> actual = await _migrationDurableFunction.GetInstanceBatchesByTimeStampAsync(
            new MigrationBatchCreationArguments(max, batchSize, maxParallelBatches, startTimeStamp, endTimeStamp),
            NullLogger.Instance);

        Assert.Same(expected, actual);
        await _instanceStore
            .Received(1)
            .GetInstanceBatchesByTimeStampAsync(batchSize, maxParallelBatches, IndexStatus.Created, startTimeStamp, endTimeStamp, max, CancellationToken.None);
    }

    [Fact]
    public async Task GivenBatch_WhenMigrating_ThenShouldMigrateEachInstance()
    {
        var args = new WatermarkRange(3, 10);

        var expected = new List<VersionedInstanceIdentifier>
        {
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 3),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 4),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 5),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 6),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 7),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 8),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 9),
        };

        // Arrange input
        _instanceStore
            .GetInstanceIdentifiersByWatermarkRangeAsync(args, IndexStatus.Created, Arg.Any<CancellationToken>())
            .Returns(expected);

        foreach (VersionedInstanceIdentifier identifier in expected)
        {
            _metadataStore.CopyInstanceFramesRangeAsync(identifier.Version).Returns(Task.CompletedTask);
            _metadataStore.DeleteMigratedFramesRangeIfExistsAsync(identifier.Version).Returns(Task.CompletedTask);
        }

        // Call the activity
        await _migrationDurableFunction.MigrateFrameRangeFilesAsync(args, NullLogger.Instance);

        // Assert behavior
        await _instanceStore
            .Received(1)
            .GetInstanceIdentifiersByWatermarkRangeAsync(args, IndexStatus.Created, CancellationToken.None);

        foreach (VersionedInstanceIdentifier identifier in expected)
        {
            await _metadataStore.Received(1).CopyInstanceFramesRangeAsync(identifier.Version, Arg.Any<CancellationToken>());
            await _metadataStore.Received(1).DeleteMigratedFramesRangeIfExistsAsync(identifier.Version, Arg.Any<CancellationToken>());
        }
    }
}
