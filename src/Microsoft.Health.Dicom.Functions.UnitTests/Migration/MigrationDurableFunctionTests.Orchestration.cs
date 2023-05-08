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
using Microsoft.Health.Dicom.Functions.Migration;
using Microsoft.Health.Dicom.Functions.Migration.Models;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Migration;

public partial class MigrationDurableFunctionTests
{
    [Fact]
    public async Task GivenNewOrchestrationWithInput_WhenMigratingInstances_ThenDivideAndMigrateBatches()
    {
        const int batchSize = 5;
        const int maxParallelBatches = 3;

        var now = DateTime.UtcNow;
        var startTimeStamp = now;
        var endTimeStamp = now.AddDays(1);

        DateTime createdTime = DateTime.UtcNow;

        var batching = new BatchingOptions
        {
            MaxParallelCount = maxParallelBatches,
            Size = batchSize,
        };

        IReadOnlyList<WatermarkRange> expectedBatches = CreateBatches(50);
        var expectedInput = new MigratingFilesCheckpoint
        {
            Batching = batching,
            StartFilterTimeStamp = startTimeStamp,
            EndFilterTimeStamp = endTimeStamp,
            CreatedTime = createdTime
        };

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);
        context
            .GetInput<MigratingFilesCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(MigrationFilesDurableFunction.GetInstanceBatchesByTimeStampAsync),
                _options.RetryOptions,
                Arg.Is<MigrationBatchCreationArguments>(x => x.StartFilterTimeStamp == now && x.EndFilterTimeStamp == now.AddDays(1)))
            .Returns(expectedBatches);
        context
            .CallActivityWithRetryAsync(
                nameof(MigrationFilesDurableFunction.MigrateFrameRangeFilesAsync),
                _options.RetryOptions,
                Arg.Any<WatermarkRange>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _migrationDurableFunction.MigrateFilesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<MigratingFilesCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(MigrationFilesDurableFunction.GetInstanceBatchesByTimeStampAsync),
                _options.RetryOptions,
                Arg.Is<MigrationBatchCreationArguments>(x => x.StartFilterTimeStamp == now && x.EndFilterTimeStamp == now.AddDays(1)));

        await context
                .Received(3)
                .CallActivityWithRetryAsync(
                    nameof(MigrationFilesDurableFunction.MigrateFrameRangeFilesAsync),
                    _options.RetryOptions,
                    Arg.Any<WatermarkRange>());

        context
            .Received(1)
            .ContinueAsNew(
                Arg.Is<MigratingFilesInput>(x => x.StartFilterTimeStamp == now && x.EndFilterTimeStamp == now.AddDays(1)),
                false);
    }

    [Fact]
    public async Task GivenNewOrchestrationWithNoBatches_WhenMigratingInstances_ThenDivideAndMigrateBatches()
    {
        const int batchSize = 5;
        const int maxParallelBatches = 3;

        var now = DateTime.UtcNow;
        var startTimeStamp = now;
        var endTimeStamp = now.AddDays(1);

        DateTime createdTime = DateTime.UtcNow;

        var batching = new BatchingOptions
        {
            MaxParallelCount = maxParallelBatches,
            Size = batchSize,
        };

        IReadOnlyList<WatermarkRange> expectedBatches = CreateBatches(0);
        var expectedInput = new MigratingFilesCheckpoint
        {
            Batching = batching,
            StartFilterTimeStamp = startTimeStamp,
            EndFilterTimeStamp = endTimeStamp,
            CreatedTime = createdTime
        };

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);
        context
            .GetInput<MigratingFilesCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(MigrationFilesDurableFunction.GetInstanceBatchesByTimeStampAsync),
                _options.RetryOptions,
                Arg.Is<MigrationBatchCreationArguments>(x => x.StartFilterTimeStamp == now && x.EndFilterTimeStamp == now.AddDays(1)))
            .Returns(expectedBatches);
        context
            .CallActivityWithRetryAsync(
                nameof(MigrationFilesDurableFunction.MigrateFrameRangeFilesAsync),
                _options.RetryOptions,
                Arg.Any<WatermarkRange>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _migrationDurableFunction.MigrateFilesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<MigratingFilesCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                nameof(MigrationFilesDurableFunction.GetInstanceBatchesByTimeStampAsync),
                _options.RetryOptions,
                Arg.Is<MigrationBatchCreationArguments>(x => x.StartFilterTimeStamp == now && x.EndFilterTimeStamp == now.AddDays(1)));

        await context
                .DidNotReceive()
                .CallActivityWithRetryAsync(
                    nameof(MigrationFilesDurableFunction.MigrateFrameRangeFilesAsync),
                    _options.RetryOptions,
                    Arg.Any<WatermarkRange>());

        context
            .DidNotReceive()
            .ContinueAsNew(
                Arg.Is<MigratingFilesInput>(x => x.StartFilterTimeStamp == now && x.EndFilterTimeStamp == now.AddDays(1)),
                false);
    }

    private static IDurableOrchestrationContext CreateContext(string operationId)
    {
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(operationId);
        return context;
    }

    private static IReadOnlyList<WatermarkRange> CreateBatches(long end, int batchSize = 5, int maxParallelBatches = 3)
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
