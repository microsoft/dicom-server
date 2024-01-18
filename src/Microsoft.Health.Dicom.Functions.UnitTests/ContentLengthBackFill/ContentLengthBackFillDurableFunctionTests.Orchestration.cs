// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.ContentLengthBackFill;
using Microsoft.Health.Dicom.Functions.ContentLengthBackFill.Models;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.ContentLengthBackFill;
public partial class ContentLengthBackFillDurableFunctionTests
{
    [Fact]
    public async Task GivenNewOrchestrationWithInput_WhenBackFilling_ThenDivideAndCleanupBatches()
    {
        const int batchSize = 5;
        const int maxParallelBatches = 3;

        DateTime createdTime = DateTime.UtcNow;

        var batching = new BatchingOptions
        {
            MaxParallelCount = maxParallelBatches,
            Size = batchSize,
        };

        IReadOnlyList<WatermarkRange> expectedBatches = CreateBatches(50, batchSize: batchSize, maxParallelBatches: maxParallelBatches);
        var expectedInput = new ContentLengthBackFillCheckPoint
        {
            Batching = batching,
            CreatedTime = createdTime
        };

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext(OperationId.Generate());

        context
            .GetInput<ContentLengthBackFillCheckPoint>()
            .Returns(expectedInput);

        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(ContentLengthBackFillDurableFunction.GetContentLengthBackFillInstanceBatches),
                _options.RetryOptions,
                Arg.Is<BatchCreationArguments>(
                    x => x.BatchSize == batchSize && x.MaxParallelBatches == maxParallelBatches))
            .Returns(expectedBatches);

        context
            .CallActivityWithRetryAsync(
                nameof(ContentLengthBackFillDurableFunction.BackFillContentLengthRangeDataAsync),
                _options.RetryOptions,
                Arg.Any<WatermarkRange>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _contentLengthBackFillDurableFunction.ContentLengthBackFillAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<ContentLengthBackFillCheckPoint>();

        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(ContentLengthBackFillDurableFunction.GetContentLengthBackFillInstanceBatches),
                _options.RetryOptions,
                Arg.Is<BatchCreationArguments>(
                    x => x.BatchSize == batchSize && x.MaxParallelBatches == maxParallelBatches));

        await context
                .Received(3)
                .CallActivityWithRetryAsync(
                    nameof(ContentLengthBackFillDurableFunction.BackFillContentLengthRangeDataAsync),
                    _options.RetryOptions,
                    Arg.Any<WatermarkRange>());

        context
            .Received(1)
            .ContinueAsNew(
                Arg.Is<ContentLengthBackFillCheckPoint>(x =>
                    x.Batching == batching &&
                    x.CreatedTime == expectedInput.CreatedTime));
    }

    [Fact]
    public async Task GivenNewOrchestrationWithInput_WhenBackFillingButNoBatchesAvailable_ThenExpectUpdateMethodsNotCalled()
    {
        const int batchSize = 5;
        const int maxParallelBatches = 3;

        DateTime createdTime = DateTime.UtcNow;

        var batching = new BatchingOptions
        {
            MaxParallelCount = maxParallelBatches,
            Size = batchSize,
        };

        IReadOnlyList<WatermarkRange> expectedBatches = CreateBatches(0, batchSize: batchSize, maxParallelBatches: maxParallelBatches);
        Assert.Empty(expectedBatches);

        var expectedInput = new ContentLengthBackFillCheckPoint
        {
            Batching = batching,
            CreatedTime = createdTime
        };

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext(OperationId.Generate());

        context
            .GetInput<ContentLengthBackFillCheckPoint>()
            .Returns(expectedInput);

        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(ContentLengthBackFillDurableFunction.GetContentLengthBackFillInstanceBatches),
                _options.RetryOptions,
                Arg.Is<BatchCreationArguments>(
                    x => x.BatchSize == batchSize && x.MaxParallelBatches == maxParallelBatches))
            .Returns(expectedBatches);

        context
            .CallActivityWithRetryAsync(
                nameof(ContentLengthBackFillDurableFunction.BackFillContentLengthRangeDataAsync),
                _options.RetryOptions,
                Arg.Any<WatermarkRange>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _contentLengthBackFillDurableFunction.ContentLengthBackFillAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<ContentLengthBackFillCheckPoint>();

        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(ContentLengthBackFillDurableFunction.GetContentLengthBackFillInstanceBatches),
                _options.RetryOptions,
                Arg.Is<BatchCreationArguments>(
                    x => x.BatchSize == batchSize && x.MaxParallelBatches == maxParallelBatches));

        await context
                .DidNotReceive()
                .CallActivityWithRetryAsync(
                    nameof(ContentLengthBackFillDurableFunction.BackFillContentLengthRangeDataAsync),
                    _options.RetryOptions,
                    Arg.Any<WatermarkRange>());

        context
            .DidNotReceive()
            .ContinueAsNew(
                Arg.Is<ContentLengthBackFillCheckPoint>(x =>
                    x.Batching == batching),
                false);
    }

    private static IDurableOrchestrationContext CreateContext(string operationId)
    {
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(operationId);
        return context;
    }

    private static IReadOnlyList<WatermarkRange> CreateBatches(long end, int batchSize, int maxParallelBatches)
    {
        var batches = new List<WatermarkRange>();

        long current = end;
        for (int i = 0; i < maxParallelBatches && current > 0; i++)
        {
            batches.Add(new WatermarkRange(Math.Max(1, current - batchSize + 1), current));
            current -= batchSize;
        }

        return batches;
    }
}
