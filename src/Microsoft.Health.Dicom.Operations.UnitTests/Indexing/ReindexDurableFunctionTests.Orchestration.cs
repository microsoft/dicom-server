// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models.Indexing;
using Microsoft.Health.Dicom.Operations.Indexing;
using Microsoft.Health.Dicom.Operations.Indexing.Models;
using Microsoft.Health.Operations;
using Microsoft.Health.Operations.Functions.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Operations.UnitTests.Indexing;

public partial class ReindexDurableFunctionTests
{
    [Fact]
    public async Task GivenNewOrchestrationWithWork_WhenReindexingInstances_ThenDivideAndReindexBatches()
    {
        const int batchSize = 5;
        _options.BatchSize = batchSize;
        _options.MaxParallelBatches = 3;

        DateTime createdTime = DateTime.UtcNow;

        IReadOnlyList<WatermarkRange> expectedBatches = CreateBatches(50);
        var expectedInput = new ReindexInput { QueryTagKeys = new List<int> { 1, 2, 3, 4, 5 } };
        var expectedTags = new List<ExtendedQueryTagStoreEntry>
        {
            new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
            new ExtendedQueryTagStoreEntry(2, "02020202", "IS", "foo", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
            new ExtendedQueryTagStoreEntry(4, "04040404", "SH", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0)
        };

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);
        context
            .GetInput<ReindexInput>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                _options.RetryOptions,
                expectedInput.QueryTagKeys)
            .Returns(expectedTags);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(ReindexDurableFunction.GetInstanceBatchesV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate((long?)null)))
            .Returns(expectedBatches);
        context
            .CallActivityWithRetryAsync(
                nameof(ReindexDurableFunction.ReindexBatchV2Async),
                _options.RetryOptions,
                Arg.Any<ReindexBatchArguments>())
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(operationId)))
            .Returns(new DurableOrchestrationStatus { CreatedTime = createdTime });

        // Invoke the orchestration
        await _reindexDurableFunction.ReindexInstancesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<ReindexInput>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                _options.RetryOptions,
                expectedInput.QueryTagKeys);
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.GetQueryTagsAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(ReindexDurableFunction.GetInstanceBatchesV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate((long?)null)));

        foreach (WatermarkRange batch in expectedBatches)
        {
            await context
                .Received(1)
                .CallActivityWithRetryAsync(
                    nameof(ReindexDurableFunction.ReindexBatchV2Async),
                    _options.RetryOptions,
                    Arg.Is(GetPredicate(expectedTags, batch)));
        }

        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                nameof(ReindexDurableFunction.CompleteReindexingAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
             .Received(1)
             .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(operationId)));
        context
            .Received(1)
            .ContinueAsNew(
                Arg.Is<ReindexInput>(x => GetPredicate(createdTime, expectedTags, expectedBatches, 50)(x)),
                false);
    }

    [Fact]
    public async Task GivenExistingOrchestrationWithWork_WhenReindexingInstances_ThenDivideAndReindexBatches()
    {
        const int batchSize = 3;
        _options.BatchSize = batchSize;
        _options.MaxParallelBatches = 2;

        IReadOnlyList<WatermarkRange> expectedBatches = CreateBatches(35);
        var expectedInput = new ReindexInput
        {
            Completed = new WatermarkRange(36, 42),
            CreatedTime = DateTime.UtcNow,
            QueryTagKeys = new List<int> { 1, 2, 3, 4, 5 },
        };
        var expectedTags = new List<ExtendedQueryTagStoreEntry>
        {
            new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
            new ExtendedQueryTagStoreEntry(2, "02020202", "IS", "foo", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
            new ExtendedQueryTagStoreEntry(4, "04040404", "SH", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0)
        };

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext();
        context
            .GetInput<ReindexInput>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.GetQueryTagsAsync),
                _options.RetryOptions,
                input: null)
            .Returns(expectedTags);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(ReindexDurableFunction.GetInstanceBatchesV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(35L)))
            .Returns(expectedBatches);
        context
            .CallActivityWithRetryAsync(
                nameof(ReindexDurableFunction.ReindexBatchV2Async),
                _options.RetryOptions,
                Arg.Any<ReindexBatchArguments>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _reindexDurableFunction.ReindexInstancesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<ReindexInput>();
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.GetQueryTagsAsync),
                _options.RetryOptions,
                input: null);
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(ReindexDurableFunction.GetInstanceBatchesV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(35L)));

        foreach (WatermarkRange batch in expectedBatches)
        {
            await context
                .Received(1)
                .CallActivityWithRetryAsync(
                    nameof(ReindexDurableFunction.ReindexBatchV2Async),
                    _options.RetryOptions,
                    Arg.Is(GetPredicate(expectedTags, batch)));
        }

        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                nameof(ReindexDurableFunction.CompleteReindexingAsync),
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
                Arg.Is<ReindexInput>(x => GetPredicate(expectedInput.CreatedTime.Value, expectedTags, expectedBatches, 42)(x)),
                false);
    }

    [Fact]
    public async Task GivenNoInstances_WhenReindexingInstances_ThenComplete()
    {
        var expectedBatches = new List<WatermarkRange>();
        var expectedInput = new ReindexInput { QueryTagKeys = new List<int> { 1, 2, 3, 4, 5 } };
        var expectedTags = new List<ExtendedQueryTagStoreEntry>
        {
            new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
            new ExtendedQueryTagStoreEntry(2, "02020202", "IS", "foo", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
            new ExtendedQueryTagStoreEntry(4, "04040404", "SH", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0)
        };

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext();
        context
            .GetInput<ReindexInput>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                _options.RetryOptions,
                expectedInput.QueryTagKeys)
            .Returns(expectedTags);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(ReindexDurableFunction.GetInstanceBatchesV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate((long?)null)))
            .Returns(expectedBatches);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                nameof(ReindexDurableFunction.CompleteReindexingAsync),
                _options.RetryOptions,
                Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTags.Select(x => x.Key))))
            .Returns(expectedTags.Select(x => x.Key).ToList());

        // Invoke the orchestration
        await _reindexDurableFunction.ReindexInstancesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<ReindexInput>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                _options.RetryOptions,
                expectedInput.QueryTagKeys);
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.GetQueryTagsAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(ReindexDurableFunction.GetInstanceBatchesV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate((long?)null)));
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(ReindexDurableFunction.ReindexBatchV2Async),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                nameof(ReindexDurableFunction.CompleteReindexingAsync),
                _options.RetryOptions,
                Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTags.Select(x => x.Key))));
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
    public async Task GivenNoRemainingInstances_WhenReindexingInstances_ThenComplete(long start, long end)
    {
        var expectedBatches = new List<WatermarkRange>();
        var expectedInput = new ReindexInput
        {
            Completed = new WatermarkRange(start, end),
            CreatedTime = DateTime.UtcNow,
            QueryTagKeys = new List<int> { 1, 2, 3, 4, 5 },
        };
        var expectedTags = new List<ExtendedQueryTagStoreEntry>
        {
            new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
            new ExtendedQueryTagStoreEntry(2, "02020202", "IS", "foo", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
            new ExtendedQueryTagStoreEntry(4, "04040404", "SH", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0)
        };

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext();
        context
            .GetInput<ReindexInput>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.GetQueryTagsAsync),
                _options.RetryOptions,
                input: null)
            .Returns(expectedTags);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(ReindexDurableFunction.GetInstanceBatchesV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(start - 1)))
            .Returns(expectedBatches);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                nameof(ReindexDurableFunction.CompleteReindexingAsync),
                _options.RetryOptions,
                Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTags.Select(x => x.Key))))
            .Returns(expectedTags.Select(x => x.Key).ToList());

        // Invoke the orchestration
        await _reindexDurableFunction.ReindexInstancesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<ReindexInput>();
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.GetQueryTagsAsync),
                _options.RetryOptions,
                input: null);
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(ReindexDurableFunction.GetInstanceBatchesV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(start - 1)));
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(ReindexDurableFunction.ReindexBatchV2Async),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                nameof(ReindexDurableFunction.CompleteReindexingAsync),
                _options.RetryOptions,
                Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTags.Select(x => x.Key))));
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

    [Fact]
    public async Task GivenNoQueryTags_WhenReindexingInstances_ThenComplete()
    {
        var expectedInput = new ReindexInput { QueryTagKeys = new List<int> { 1, 2, 3, 4, 5 } };
        var expectedTags = new List<ExtendedQueryTagStoreEntry>();

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext();
        context
            .GetInput<ReindexInput>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                _options.RetryOptions,
                expectedInput.QueryTagKeys)
            .Returns(expectedTags);

        // Invoke the orchestration
        await _reindexDurableFunction.ReindexInstancesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<ReindexInput>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                _options.RetryOptions,
                expectedInput.QueryTagKeys);
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.GetQueryTagsAsync),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(ReindexDurableFunction.GetInstanceBatchesV2Async),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(ReindexDurableFunction.ReindexBatchV2Async),
                _options.RetryOptions,
                Arg.Any<object>());
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                nameof(ReindexDurableFunction.CompleteReindexingAsync),
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
        for (int i = 0; i < _options.MaxParallelBatches && current > 0; i++)
        {
            batches.Add(new WatermarkRange(Math.Max(1, current - _options.BatchSize + 1), current));
            current -= _options.BatchSize;
        }

        return batches;
    }

    private Expression<Predicate<BatchCreationArguments>> GetPredicate(long? maxWatermark)
    {
        return x => x.MaxWatermark == maxWatermark
            && x.BatchSize == _options.BatchSize
            && x.MaxParallelBatches == _options.MaxParallelBatches;
    }

    private Expression<Predicate<ReindexBatchArguments>> GetPredicate(
        IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags,
        WatermarkRange expected)
    {
        return x => ReferenceEquals(x.QueryTags, queryTags)
            && x.WatermarkRange == expected
            && x.ThreadCount == _options.BatchThreadCount;
    }

    private static Expression<Predicate<GetInstanceStatusInput>> GetPredicate(string instanceId)
    {
        return x => x.InstanceId == instanceId
            && !x.ShowHistory
            && !x.ShowHistoryOutput
            && !x.ShowInput;
    }

    private static Predicate<object> GetPredicate(
        DateTime createdTime,
        IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags,
        IReadOnlyList<WatermarkRange> expectedBatches,
        long end)
    {
        return x => x is ReindexInput r
            && r.QueryTagKeys.SequenceEqual(queryTags.Select(y => y.Key))
            && r.Completed == new WatermarkRange(expectedBatches[expectedBatches.Count - 1].Start, end)
            && r.CreatedTime == createdTime;
    }
}
