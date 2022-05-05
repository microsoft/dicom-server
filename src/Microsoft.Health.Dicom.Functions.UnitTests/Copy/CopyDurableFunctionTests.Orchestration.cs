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
using Microsoft.Health.Dicom.Core.Models.Copy;
using Microsoft.Health.Dicom.Functions.Copy;
using Microsoft.Health.Dicom.Functions.Indexing.Models;
using Microsoft.Health.Operations;
using Microsoft.Health.Operations.Functions.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Copy;

public partial class CopyDurableFunctionTests
{
    [Fact]
    public async Task GivenNewOrchestrationWithWork_WhenCopyingInstances_ThenDivideAndDuplicateBatches()
    {
        DateTime createdTime = DateTime.UtcNow;

        IReadOnlyList<WatermarkRange> expectedBatches = CreateBatches(50);
        var expectedInput = new CopyCheckpoint();
        expectedInput.Batching = _batchingOptions;

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);
        context
            .GetInput<CopyCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(CopyDurableFunction.GetCopyInstanceBatchesAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(null)))
            .Returns(expectedBatches);
        context
            .CallActivityWithRetryAsync(
                nameof(CopyDurableFunction.CopyBatchAsync),
                _options.RetryOptions,
                Arg.Any<WatermarkRange>())
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate()))
            .Returns(new DurableOrchestrationStatus { CreatedTime = createdTime });

        // Invoke the orchestration
        await _function.CopyInstancesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<CopyCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(CopyDurableFunction.GetCopyInstanceBatchesAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(null)));

        foreach (WatermarkRange batch in expectedBatches)
        {
            await context
                .Received(1)
                .CallActivityWithRetryAsync(
                    nameof(CopyDurableFunction.CopyBatchAsync),
                    _options.RetryOptions,
                    Arg.Is(batch));
        }

        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                nameof(CopyDurableFunction.CompleteCopyAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
             .Received(1)
             .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate()));
        context
            .Received(1)
            .ContinueAsNew(
                Arg.Is<CopyCheckpoint>(x => GetPredicate(createdTime, expectedBatches, 50)(x)),
                false);
    }

    [Fact]
    public async Task GivenExistingOrchestrationWithWork_WhenCopyingInstances_ThenDivideAndCopyBatches()
    {
        IReadOnlyList<WatermarkRange> expectedBatches = CreateBatches(35);
        var expectedInput = new CopyCheckpoint
        {
            Completed = new WatermarkRange(36, 42),
            CreatedTime = DateTime.UtcNow,
            Batching = _batchingOptions
        };

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext();
        context
            .GetInput<CopyCheckpoint>()
            .Returns(expectedInput);

        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(CopyDurableFunction.GetCopyInstanceBatchesAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(35L)))
            .Returns(expectedBatches);
        context
            .CallActivityWithRetryAsync(
                nameof(CopyDurableFunction.CopyBatchAsync),
                _options.RetryOptions,
                Arg.Any<WatermarkRange>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _function.CopyInstancesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<CopyCheckpoint>();

        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(CopyDurableFunction.GetCopyInstanceBatchesAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(35L)));

        foreach (WatermarkRange batch in expectedBatches)
        {
            await context
                .Received(1)
                .CallActivityWithRetryAsync(
                    nameof(CopyDurableFunction.CopyBatchAsync),
                    _options.RetryOptions,
                    Arg.Is(batch));
        }

        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                nameof(CopyDurableFunction.CompleteCopyAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
             .DidNotReceive()
             .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        context
            .Received(1)
            .ContinueAsNew(
                Arg.Is<CopyCheckpoint>(x => GetPredicate(expectedInput.CreatedTime.Value, expectedBatches, 42)(x)),
                false);
    }

    [Fact]
    public async Task GivenNoInstances_WhenCopyingInstances_ThenComplete()
    {
        var expectedBatches = new List<WatermarkRange>();
        var expectedInput = new CopyCheckpoint();
        expectedInput.Batching = _batchingOptions;

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext();
        context
            .GetInput<CopyCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(CopyDurableFunction.GetCopyInstanceBatchesAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(null)))
            .Returns(expectedBatches);

        // Invoke the orchestration
        await _function.CopyInstancesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<CopyCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(CopyDurableFunction.GetCopyInstanceBatchesAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(null)));
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(CopyDurableFunction.CopyBatchAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(CopyDurableFunction.CompleteCopyAsync),
                _options.RetryOptions,
                Arg.Any<object>());

        await context
             .DidNotReceive()
             .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        context
            .DidNotReceiveWithAnyArgs()
            .ContinueAsNew(default, default);
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(5, 1000)]
    public async Task GivenNoRemainingInstances_WhenCopyingInstances_ThenComplete(long start, long end)
    {
        var expectedBatches = new List<WatermarkRange>();
        var expectedInput = new CopyCheckpoint
        {
            Completed = new WatermarkRange(start, end),
            CreatedTime = DateTime.UtcNow,
            Batching = _batchingOptions
        };

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext();
        context
            .GetInput<CopyCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(CopyDurableFunction.GetCopyInstanceBatchesAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(start - 1)))
            .Returns(expectedBatches);

        // Invoke the orchestration
        await _function.CopyInstancesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<CopyCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(CopyDurableFunction.GetCopyInstanceBatchesAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(start - 1)));
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(CopyDurableFunction.CopyBatchAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(CopyDurableFunction.CompleteCopyAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
             .DidNotReceive()
             .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        context
            .DidNotReceiveWithAnyArgs()
            .ContinueAsNew(default, default);
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
        for (int i = 0; i < _batchingOptions.MaxParallelCount && current > 0; i++)
        {
            batches.Add(new WatermarkRange(Math.Max(1, current - _batchingOptions.Size + 1), current));
            current -= _batchingOptions.Size;
        }

        return batches;
    }

    private Expression<Predicate<BatchCreationArguments>> GetPredicate(long? maxWatermark)
    {
        return x => x.MaxWatermark == maxWatermark
            && x.BatchSize == _batchingOptions.Size
            && x.MaxParallelBatches == _batchingOptions.MaxParallelCount;
    }

    private static Expression<Predicate<GetInstanceStatusOptions>> GetPredicate()
    {
        return x => !x.ShowHistory && !x.ShowHistoryOutput && !x.ShowInput;
    }

    private static Predicate<object> GetPredicate(
        DateTime createdTime,
        IReadOnlyList<WatermarkRange> expectedBatches,
        long end)
    {
        return x => x is CopyCheckpoint r
            && r.Completed == new WatermarkRange(expectedBatches[expectedBatches.Count - 1].Start, end)
            && r.CreatedTime == createdTime;
    }
}
