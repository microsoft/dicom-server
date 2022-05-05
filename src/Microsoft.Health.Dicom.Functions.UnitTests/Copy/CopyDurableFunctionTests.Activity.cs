// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Functions.Copy.Models;
using Microsoft.Health.Dicom.Functions.Indexing.Models;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Copy;

public partial class CopyDurableFunctionTests
{
    [Fact]
    public async Task GivenNoWatermark_WhenGettingInstanceBatches_ThenShouldInvokeCorrectMethod()
    {
        const int batchSize = 100;
        const int maxParallelBatches = 3;

        IReadOnlyList<WatermarkRange> expected = new List<WatermarkRange> { new WatermarkRange(12345, 678910) };
        _instanceStore
            .GetInstanceBatchesAsync(batchSize, maxParallelBatches, IndexStatus.Created, null, CancellationToken.None)
            .Returns(expected);

        IReadOnlyList<WatermarkRange> actual = await _function.GetCopyInstanceBatchesAsync(
            new BatchCreationArguments(null, batchSize, maxParallelBatches),
            NullLogger.Instance);

        Assert.Same(expected, actual);
        await _instanceStore
            .Received(1)
            .GetInstanceBatchesAsync(batchSize, maxParallelBatches, IndexStatus.Created, null, CancellationToken.None);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public async Task GivenWatermark_WhenGettingInstanceBatches_ThenShouldInvokeCorrectMethod(long max)
    {
        const int batchSize = 100;
        const int maxParallelBatches = 3;

        IReadOnlyList<WatermarkRange> expected = new List<WatermarkRange> { new WatermarkRange(1, 2) }; // watermarks don't matter
        _instanceStore
            .GetInstanceBatchesAsync(batchSize, maxParallelBatches, IndexStatus.Created, max, CancellationToken.None)
            .Returns(expected);

        IReadOnlyList<WatermarkRange> actual = await _function.GetCopyInstanceBatchesAsync(
            new BatchCreationArguments(max, batchSize, maxParallelBatches),
            NullLogger.Instance);

        Assert.Same(expected, actual);
        await _instanceStore
            .Received(1)
            .GetInstanceBatchesAsync(batchSize, maxParallelBatches, IndexStatus.Created, max, CancellationToken.None);
    }



    [Fact]
    public async Task GivenBatch_WhenDuplicateing_ThenShouldDuplicateEachInstance()
    {
        const int threadCount = 7;
        var args = new CopyBatchArguments(
            new WatermarkRange(3, 10),
            threadCount);

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
            .GetInstanceIdentifiersByWatermarkRangeAsync(args.WatermarkRange, IndexStatus.Created, CancellationToken.None)
            .Returns(expected);


        // Call the activity
        await _function.CopyBatchAsync(args, NullLogger.Instance);

        // Assert behavior
        await _instanceStore
            .Received(1)
            .GetInstanceIdentifiersByWatermarkRangeAsync(args.WatermarkRange, IndexStatus.Created, CancellationToken.None);

        foreach (VersionedInstanceIdentifier identifier in expected)
        {
            await _instanceCopier.Received(1).DuplicateInstanceAsync(identifier);
        }
    }
}
