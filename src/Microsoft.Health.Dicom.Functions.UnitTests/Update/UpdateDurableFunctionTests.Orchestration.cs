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
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Functions.Update;
using Microsoft.Health.Dicom.Functions.Update.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Operations;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenTelemetry.Metrics;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Update;

public partial class UpdateDurableFunctionTests
{
    [Fact]
    public async Task GivenNewOrchestrationWithInput_WhenUpdatingInstances_ThenComplete()
    {
        const int batchSize = 5;
        _options.BatchSize = batchSize;

        DateTime createdTime = DateTime.UtcNow;

        var expectedInput = GetUpdateCheckpoint();

        var expectedInstances = new List<InstanceFileState>
        {
            new InstanceFileState
            {
                Version = 1
            },
            new InstanceFileState
            {
                Version = 2
            }
        };

        var expectedInstancesWithNewWatermark = new List<InstanceFileState>
        {
            new InstanceFileState
            {
                Version = 1,
                NewVersion = 3,
            },
            new InstanceFileState
            {
                Version = 2,
                NewVersion = 4,
            }
        };

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);

        context
            .GetInput<UpdateCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceFileState>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkAsync),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArguments>())
            .Returns(expectedInstancesWithNewWatermark);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(DefaultPartition.Key, expectedInstancesWithNewWatermark, expectedInput.ChangeDataset)))
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyAsync),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArguments>())
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.DeleteOldVersionBlobAsync),
                _options.RetryOptions,
                expectedInstances)
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _updateDurableFunction.UpdateInstancesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<UpdateCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceFileState>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkAsync),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArguments>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsAsync),
                _options.RetryOptions,
               Arg.Is(GetPredicate(DefaultPartition.Key, expectedInstancesWithNewWatermark, expectedInput.ChangeDataset)));
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyAsync),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArguments>());
        context
            .Received(1)
            .ContinueAsNew(
                Arg.Is<UpdateCheckpoint>(x => x.NumberOfStudyCompleted == 1),
                false);
    }

    [Fact]
    public async Task GivenNewOrchestrationWithNoInstancesFound_WhenUpdatingInstances_ThenComplete()
    {
        const int batchSize = 5;
        _options.BatchSize = batchSize;

        DateTime createdTime = DateTime.UtcNow;

        var expectedInput = new UpdateCheckpoint
        {
            PartitionKey = DefaultPartition.Key,
            ChangeDataset = string.Empty,
            StudyInstanceUids = new List<string> {
                TestUidGenerator.Generate()
            },
            CreatedTime = createdTime,
        };

        var expectedInstances = new List<InstanceFileState>();

        var expectedInstancesWithNewWatermark = new List<InstanceFileState>();

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);

        context
            .GetInput<UpdateCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceFileState>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkAsync),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArguments>())
            .Returns(expectedInstancesWithNewWatermark);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(DefaultPartition.Key, expectedInstancesWithNewWatermark, expectedInput.ChangeDataset)))
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyAsync),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArguments>())
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.DeleteOldVersionBlobAsync),
                _options.RetryOptions,
                expectedInstances)
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _updateDurableFunction.UpdateInstancesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<UpdateCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceFileState>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkAsync),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArguments>());
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsAsync),
                _options.RetryOptions,
               Arg.Is(GetPredicate(DefaultPartition.Key, expectedInstancesWithNewWatermark, expectedInput.ChangeDataset)));
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyAsync),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArguments>());
        context
            .Received(1)
            .ContinueAsNew(
                Arg.Any<UpdateCheckpoint>(),
                false);

        _meterProvider.ForceFlush();
        Assert.Empty(_exportedItems.Where(item => item.Name.Equals(_updateMeter.UpdatedInstances.Name, StringComparison.Ordinal)));
    }


    [Fact]
    public async Task GivenNewOrchestrationWithInput_WhenUpdatingInstancesWithException_ThenFails()
    {
        const int batchSize = 5;
        _options.BatchSize = batchSize;

        DateTime createdTime = DateTime.UtcNow;

        var expectedInput = new UpdateCheckpoint
        {
            PartitionKey = DefaultPartition.Key,
            ChangeDataset = string.Empty,
            StudyInstanceUids = new List<string>(),
            CreatedTime = createdTime,
            Errors = new List<string>()
            {
                "Failed Study"
            }
        };

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);

        context
            .GetInput<UpdateCheckpoint>()
            .Returns(expectedInput);

        // Invoke the orchestration
        await Assert.ThrowsAsync<OperationErrorException>(() => _updateDurableFunction.UpdateInstancesAsync(context, NullLogger.Instance));

        // Assert behavior
        context
            .Received(1)
            .GetInput<UpdateCheckpoint>();
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceFileState>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkAsync),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArguments>());
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceFileState>>(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsAsync),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceBlobArguments>());
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyAsync),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArguments>());
        context
            .DidNotReceive()
            .ContinueAsNew(
                Arg.Any<UpdateCheckpoint>(),
                false);

        _meterProvider.ForceFlush();
        Assert.Empty(_exportedItems.Where(item => item.Name.Equals(_updateMeter.UpdatedInstances.Name, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task GivenNewOrchestrationWithInput_WhenUpdatingInstancesWithException_ThenCallCleanupActivity()
    {
        const int batchSize = 5;
        _options.BatchSize = batchSize;

        DateTime createdTime = DateTime.UtcNow;

        var expectedInput = new UpdateCheckpoint
        {
            PartitionKey = DefaultPartition.Key,
            ChangeDataset = string.Empty,
            StudyInstanceUids = new List<string> {
                TestUidGenerator.Generate()
            },
            CreatedTime = createdTime,
        };

        var expectedInstancesWithNewWatermark = new List<InstanceFileState>
        {
            new InstanceFileState
            {
                Version = 1,
                NewVersion = 3,
            },
            new InstanceFileState
            {
                Version = 2,
                NewVersion = 4,
            }
        };

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);

        context
            .GetInput<UpdateCheckpoint>()
            .Returns(expectedInput);

        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceFileState>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkAsync),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArguments>())
            .Returns(expectedInstancesWithNewWatermark);

        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(DefaultPartition.Key, expectedInstancesWithNewWatermark, expectedInput.ChangeDataset)))
            .ThrowsAsync(new FunctionFailedException("Function failed"));

        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CleanupNewVersionBlobAsync),
                _options.RetryOptions,
                Arg.Any<List<InstanceFileState>>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _updateDurableFunction.UpdateInstancesAsync(context, NullLogger.Instance);

        // Assert behavior
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CleanupNewVersionBlobAsync),
                _options.RetryOptions,
                Arg.Any<List<InstanceFileState>>());

        _meterProvider.ForceFlush();
        Assert.Empty(_exportedItems.Where(item => item.Name.Equals(_updateMeter.UpdatedInstances.Name, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task GivenNewOrchestrationWithInput_WhenUpdatingInstances_ThenCompleteWithUpdateProgress()
    {
        const int batchSize = 5;
        _options.BatchSize = batchSize;

        DateTime createdTime = DateTime.UtcNow;

        var expectedInput = GetUpdateCheckpoint();

        var expectedInstances = new List<InstanceFileState>
        {
            new InstanceFileState
            {
                Version = 1
            },
            new InstanceFileState
            {
                Version = 2
            }
        };

        var expectedInstancesWithNewWatermark = new List<InstanceFileState>
        {
            new InstanceFileState
            {
                Version = 1,
                NewVersion = 3,
            },
            new InstanceFileState
            {
                Version = 2,
                NewVersion = 4,
            }
        };

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);

        context
            .GetInput<UpdateCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceFileState>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkAsync),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArguments>())
            .Returns(expectedInstancesWithNewWatermark);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate(DefaultPartition.Key, expectedInstancesWithNewWatermark, expectedInput.ChangeDataset)))
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyAsync),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArguments>())
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.DeleteOldVersionBlobAsync),
                _options.RetryOptions,
                expectedInstances)
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _updateDurableFunction.UpdateInstancesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<UpdateCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceFileState>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkAsync),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArguments>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsAsync),
                _options.RetryOptions,
               Arg.Is(GetPredicate(DefaultPartition.Key, expectedInstancesWithNewWatermark, expectedInput.ChangeDataset)));
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyAsync),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArguments>());
        context
            .Received(1)
            .ContinueAsNew(
                 Arg.Is(GetPredicate(expectedInstancesWithNewWatermark.Count, 1)),
                false);
    }


    private static IDurableOrchestrationContext CreateContext()
        => CreateContext(OperationId.Generate());

    private static UpdateCheckpoint GetUpdateCheckpoint()
        => new UpdateCheckpoint
        {
            PartitionKey = DefaultPartition.Key,
            ChangeDataset = string.Empty,
            StudyInstanceUids = new List<string> {
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate()
            },
            CreatedTime = DateTime.UtcNow,
        };

    private static IDurableOrchestrationContext CreateContext(string operationId)
    {
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(operationId);
        return context;
    }

    private static Expression<Predicate<UpdateInstanceBlobArguments>> GetPredicate(int partitionKey, IReadOnlyList<InstanceFileState> instanceWatermarks, string changeDataset)
    {
        return x => x.PartitionKey == partitionKey
            && x.InstanceWatermarks == instanceWatermarks
            && x.ChangeDataset == changeDataset;
    }

    private static Expression<Predicate<UpdateCheckpoint>> GetPredicate(long instanceUpdated, int studyCompleted)
    {
        return r => r.TotalNumberOfInstanceUpdated == instanceUpdated
        && r.NumberOfStudyCompleted == studyCompleted;
    }
}
