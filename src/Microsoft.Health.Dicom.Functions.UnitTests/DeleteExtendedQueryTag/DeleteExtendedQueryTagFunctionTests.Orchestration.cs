// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag;
using Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag.Models;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.DeleteExtendedQueryTag;

public partial class DeleteExtendedQueryTagFunctionTests
{
    [Fact]
    public async Task GivenNewOrchestrationWithWork_WhenDeletingExtendedQueryTag_ThenDivideAndDeleteBatches()
    {
        _options.BatchSize = 5;
        _options.MaxParallelThreads = 3;

        DateTime createdTime = DateTime.UtcNow;

        var expectedInput = new DeleteExtendedQueryTagCheckpoint
        {
            TagKey = 1,
            VR = "US",
            Batching = new BatchingOptions
            {
                Size = _options.BatchSize,
                MaxParallelCount = _options.MaxParallelThreads,
            },
            CreatedTime = createdTime,
        };

        IReadOnlyList<WatermarkRange> expectedBatches = CreateBatches(50);

        IDurableOrchestrationContext context = CreateContext();
        context
            .GetInput<DeleteExtendedQueryTagCheckpoint>()
            .Returns(expectedInput);

        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(DeleteExtendedQueryTagFunction.GetExtendedQueryTagBatchesAsync),
                _options.RetryOptions,
                Arg.Is<BatchCreationArguments>(b => b.TagKey == expectedInput.TagKey && b.VR == expectedInput.VR && b.BatchSize == expectedInput.Batching.Size && b.BatchCount == expectedInput.Batching.MaxParallelCount))
            .Returns(expectedBatches);
        context
            .CallActivityWithRetryAsync(
                nameof(DeleteExtendedQueryTagFunction.DeleteExtendedQueryTagDataByWatermarkRangeAsync),
                _options.RetryOptions,
                Arg.Any<DeleteBatchArguments>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _deleteExtendedQueryTagFunction.DeleteExtendedQueryTagAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<DeleteExtendedQueryTagCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(DeleteExtendedQueryTagFunction.GetExtendedQueryTagBatchesAsync),
                _options.RetryOptions,
                Arg.Is<BatchCreationArguments>(b => b.TagKey == expectedInput.TagKey && b.VR == expectedInput.VR && b.BatchSize == expectedInput.Batching.Size && b.BatchCount == expectedInput.Batching.MaxParallelCount));

        foreach (WatermarkRange batch in expectedBatches)
        {
            await context
                .Received(1)
                .CallActivityWithRetryAsync(
                    nameof(DeleteExtendedQueryTagFunction.DeleteExtendedQueryTagDataByWatermarkRangeAsync),
                    _options.RetryOptions,
                    Arg.Is(GetPredicate(expectedInput.TagKey, expectedInput.VR, batch)));
        }

        await context
        .DidNotReceive()
        .CallActivityWithRetryAsync(
            nameof(DeleteExtendedQueryTagFunction.DeleteExtendedQueryTagEntry),
            _options.RetryOptions,
            Arg.Is<DeleteExtendedQueryTagArguments>(d => d.TagKey == expectedInput.TagKey));

        context
            .Received(1)
            .ContinueAsNew(
                Arg.Is<DeleteExtendedQueryTagCheckpoint>(x => GetPredicate(expectedBatches, expectedInput.TagKey, expectedInput.VR, 50)(x)),
                false);
    }

    [Fact]
    public async Task GivingExistingOrchestrationWithWork_WhenDeletingExtendedQueryTags_ThenDivideAndDeleteBatches()
    {
        _options.BatchSize = 3;
        _options.MaxParallelThreads = 2;

        DateTime createdTime = DateTime.UtcNow;

        var expectedInput = new DeleteExtendedQueryTagCheckpoint
        {
            TagKey = 1,
            VR = "US",
            Batching = new BatchingOptions
            {
                Size = _options.BatchSize,
                MaxParallelCount = _options.MaxParallelThreads,
            },
            CreatedTime = createdTime,
            Completed = new WatermarkRange(38, 49),
        };

        IReadOnlyList<WatermarkRange> expectedBatches = CreateBatches(37);

        IDurableOrchestrationContext context = CreateContext();
        context
            .GetInput<DeleteExtendedQueryTagCheckpoint>()
            .Returns(expectedInput);

        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(DeleteExtendedQueryTagFunction.GetExtendedQueryTagBatchesAsync),
                _options.RetryOptions,
                Arg.Is<BatchCreationArguments>(b => b.TagKey == expectedInput.TagKey && b.VR == expectedInput.VR && b.BatchSize == expectedInput.Batching.Size && b.BatchCount == expectedInput.Batching.MaxParallelCount))
            .Returns(expectedBatches);
        context
            .CallActivityWithRetryAsync(
                nameof(DeleteExtendedQueryTagFunction.DeleteExtendedQueryTagDataByWatermarkRangeAsync),
                _options.RetryOptions,
                Arg.Any<DeleteBatchArguments>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _deleteExtendedQueryTagFunction.DeleteExtendedQueryTagAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<DeleteExtendedQueryTagCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(DeleteExtendedQueryTagFunction.GetExtendedQueryTagBatchesAsync),
                _options.RetryOptions,
                Arg.Is<BatchCreationArguments>(b => b.TagKey == expectedInput.TagKey && b.VR == expectedInput.VR && b.BatchSize == expectedInput.Batching.Size && b.BatchCount == expectedInput.Batching.MaxParallelCount));

        foreach (WatermarkRange batch in expectedBatches)
        {
            await context
                .Received(1)
                .CallActivityWithRetryAsync(
                    nameof(DeleteExtendedQueryTagFunction.DeleteExtendedQueryTagDataByWatermarkRangeAsync),
                    _options.RetryOptions,
                    Arg.Is(GetPredicate(expectedInput.TagKey, expectedInput.VR, batch)));
        }

        await context
        .DidNotReceive()
        .CallActivityWithRetryAsync(
            nameof(DeleteExtendedQueryTagFunction.DeleteExtendedQueryTagEntry),
            _options.RetryOptions,
            Arg.Is<DeleteExtendedQueryTagArguments>(d => d.TagKey == expectedInput.TagKey));

        context
            .Received(1)
            .ContinueAsNew(
                Arg.Is<DeleteExtendedQueryTagCheckpoint>(x => GetPredicate(expectedBatches, expectedInput.TagKey, expectedInput.VR, 49)(x)),
                false);
    }

    [Fact]
    public async Task GivenNoTagDataToDelete_WhenDeleteingExtendedQueryTag_ThenCompleteAndDeleteTag()
    {
        _options.BatchSize = 3;
        _options.MaxParallelThreads = 2;

        DateTime createdTime = DateTime.UtcNow;

        var expectedInput = new DeleteExtendedQueryTagCheckpoint
        {
            TagKey = 1,
            VR = "US",
            Batching = new BatchingOptions
            {
                Size = _options.BatchSize,
                MaxParallelCount = _options.MaxParallelThreads,
            },
            CreatedTime = createdTime,
        };

        IReadOnlyList<WatermarkRange> expectedBatches = new List<WatermarkRange>();

        IDurableOrchestrationContext context = CreateContext();
        context
            .GetInput<DeleteExtendedQueryTagCheckpoint>()
            .Returns(expectedInput);

        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(DeleteExtendedQueryTagFunction.GetExtendedQueryTagBatchesAsync),
                _options.RetryOptions,
                Arg.Is<BatchCreationArguments>(b => b.TagKey == expectedInput.TagKey && b.VR == expectedInput.VR && b.BatchSize == expectedInput.Batching.Size && b.BatchCount == expectedInput.Batching.MaxParallelCount))
            .Returns(expectedBatches);
        context
            .CallActivityWithRetryAsync(
                nameof(DeleteExtendedQueryTagFunction.DeleteExtendedQueryTagEntry),
                _options.RetryOptions,
                Arg.Any<DeleteExtendedQueryTagArguments>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _deleteExtendedQueryTagFunction.DeleteExtendedQueryTagAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<DeleteExtendedQueryTagCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(DeleteExtendedQueryTagFunction.GetExtendedQueryTagBatchesAsync),
                _options.RetryOptions,
                Arg.Is<BatchCreationArguments>(b => b.TagKey == expectedInput.TagKey && b.VR == expectedInput.VR && b.BatchSize == expectedInput.Batching.Size && b.BatchCount == expectedInput.Batching.MaxParallelCount));
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(DeleteExtendedQueryTagFunction.DeleteExtendedQueryTagDataByWatermarkRangeAsync),
                _options.RetryOptions,
                Arg.Any<DeleteBatchArguments>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(DeleteExtendedQueryTagFunction.DeleteExtendedQueryTagEntry),
                _options.RetryOptions,
                Arg.Is<DeleteExtendedQueryTagArguments>(d => d.TagKey == expectedInput.TagKey));
        context
            .DidNotReceive()
            .ContinueAsNew(
                Arg.Is<DeleteExtendedQueryTagCheckpoint>(x => GetPredicate(expectedBatches, expectedInput.TagKey, expectedInput.VR, 49)(x)),
                false);
    }

    private static IDurableOrchestrationContext CreateContext()
    => CreateContext(OperationId.Generate());

    private static IDurableOrchestrationContext CreateContext(string operationId)
    {
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(operationId);
        return context;
    }

    private IReadOnlyList<WatermarkRange> CreateBatches(long end)
    {
        var batches = new List<WatermarkRange>();

        long current = end;
        for (int i = 0; i < _options.MaxParallelThreads && current > 0; i++)
        {
            batches.Add(new WatermarkRange(Math.Max(1, current - _options.BatchSize + 1), current));
            current -= _options.BatchSize;
        }

        return batches;
    }

    private static Expression<Predicate<DeleteBatchArguments>> GetPredicate(
        int tagKey,
        string VR,
        WatermarkRange expected)
    {
        return x => x.TagKey == tagKey && x.VR == VR && x.Range == expected;
    }

    private static Predicate<object> GetPredicate(
        IReadOnlyList<WatermarkRange> expectedBatches,
        int tagKey,
        string vr,
        long end)
    {
        return x => x is DeleteExtendedQueryTagCheckpoint r
            && r.Completed == new WatermarkRange(expectedBatches[expectedBatches.Count - 1].Start, end)
            && r.TagKey == tagKey
            && r.VR == vr;
    }
}
