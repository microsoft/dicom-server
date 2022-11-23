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
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.BlobMigration;
using Microsoft.Health.Dicom.Functions.BlobMigration.Models;
using Microsoft.Health.Dicom.Functions.Migration;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Operations;
using Microsoft.Health.Operations.Functions.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.CleanupDeleted;

public partial class CleanupDeletedDurableFunctionTests
{
    [Fact]
    public async Task GivenNewOrchestrationWithWork_WhenCleaningUpInstances_ThenDivideAndDeleteBatches()
    {
        DateTime createdTime = DateTime.UtcNow;
        long maxWatermark = 2;

        var identifiers = new List<VersionedInstanceIdentifier>
        {
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 2)
        };

        IReadOnlyList<ChangeFeedEntry> expectedBatches = identifiers.Select(x =>
            new ChangeFeedEntry(x.Version, DateTime.Now, ChangeFeedAction.Delete, x.StudyInstanceUid, x.SeriesInstanceUid, x.SopInstanceUid, x.Version, x.Version, ChangeFeedState.Current)
        ).ToList();

        var expectedInput = new BlobMigrationCheckpoint();
        expectedInput.Batching = _batchingOptions;
        expectedInput.FilterTimeStamp = createdTime;

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);
        context
            .GetInput<BlobMigrationCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<ChangeFeedEntry>>(
                nameof(CleanupDeletedDurableFunction.GetDeletedChangeFeedInstanceBatchesAsync),
                _options.RetryOptions,
               Arg.Any<CleanupDeletedBatchArguments>())
            .Returns(expectedBatches);
        context
           .CallActivityWithRetryAsync<long>(
               nameof(CleanupDeletedDurableFunction.GetMaxDeletedChangeFeedWatermarkAsync),
               _options.RetryOptions,
               Arg.Any<CleanupDeletedBatchArguments>())
           .Returns(maxWatermark);
        context
            .CallActivityWithRetryAsync(
                nameof(CleanupDeletedDurableFunction.CleanupDeletedBatchAsync),
                _options.RetryOptions,
                identifiers)
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate()))
            .Returns(new DurableOrchestrationStatus { CreatedTime = createdTime });

        // Invoke the orchestration
        await _function.CleanupDeletedFilesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<BlobMigrationCheckpoint>();

        await context
            .Received(1)
            .CallActivityWithRetryAsync<long>(
                nameof(CleanupDeletedDurableFunction.GetMaxDeletedChangeFeedWatermarkAsync),
                _options.RetryOptions,
               Arg.Any<CleanupDeletedBatchArguments>());

        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<ChangeFeedEntry>>(
                nameof(CleanupDeletedDurableFunction.GetDeletedChangeFeedInstanceBatchesAsync),
                _options.RetryOptions,
               Arg.Any<CleanupDeletedBatchArguments>());

        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(CleanupDeletedDurableFunction.CleanupDeletedBatchAsync),
                _options.RetryOptions,
                Arg.Any<IEnumerable<VersionedInstanceIdentifier>>());
    }

    [Fact]
    public async Task GivenExistingOrchestrationWithWatermark_WhenCleaningUpInstances_ThenDivideAndDeleteBatches()
    {
        DateTime createdTime = DateTime.UtcNow;
        long maxWatermark = 2;

        var identifiers = new List<VersionedInstanceIdentifier>
        {
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 2)
        };

        IReadOnlyList<ChangeFeedEntry> expectedBatches = identifiers.Select(x =>
            new ChangeFeedEntry(x.Version, DateTime.Now, ChangeFeedAction.Delete, x.StudyInstanceUid, x.SeriesInstanceUid, x.SopInstanceUid, x.Version, x.Version, ChangeFeedState.Current)
        ).ToList();

        var expectedInput = new BlobMigrationCheckpoint();
        expectedInput.Batching = _batchingOptions;
        expectedInput.Completed = new WatermarkRange(1, 2);
        expectedInput.MaxWatermark = maxWatermark;

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);
        context
            .GetInput<BlobMigrationCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<ChangeFeedEntry>>(
                nameof(CleanupDeletedDurableFunction.GetDeletedChangeFeedInstanceBatchesAsync),
                _options.RetryOptions,
               Arg.Any<CleanupDeletedBatchArguments>())
            .Returns(expectedBatches);

        context
            .CallActivityWithRetryAsync(
                nameof(CleanupDeletedDurableFunction.CleanupDeletedBatchAsync),
                _options.RetryOptions,
                identifiers)
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate()))
            .Returns(new DurableOrchestrationStatus { CreatedTime = createdTime });

        // Invoke the orchestration
        await _function.CleanupDeletedFilesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<BlobMigrationCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<ChangeFeedEntry>>(
                nameof(CleanupDeletedDurableFunction.GetDeletedChangeFeedInstanceBatchesAsync),
                _options.RetryOptions,
               Arg.Any<CleanupDeletedBatchArguments>());
        await context
            .Received(0)
            .CallActivityWithRetryAsync<long>(
                nameof(CleanupDeletedDurableFunction.GetMaxDeletedChangeFeedWatermarkAsync),
                _options.RetryOptions,
               Arg.Any<CleanupDeletedBatchArguments>());

        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(CleanupDeletedDurableFunction.CleanupDeletedBatchAsync),
                _options.RetryOptions,
                Arg.Any<IEnumerable<VersionedInstanceIdentifier>>());
    }

    [Fact]
    public async Task GivenExistingOrchestrationWithWatermark_WhenCleaningUpInstances_ThenCompleteDeletingFiles()
    {
        DateTime createdTime = DateTime.UtcNow;
        long maxWatermark = 2;

        var identifiers = new List<VersionedInstanceIdentifier>
        {
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 2)
        };

        var expectedInput = new BlobMigrationCheckpoint();
        expectedInput.Batching = _batchingOptions;
        expectedInput.Completed = new WatermarkRange(3, 4);
        expectedInput.MaxWatermark = maxWatermark;

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);
        context
            .GetInput<BlobMigrationCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<ChangeFeedEntry>>(
                nameof(CleanupDeletedDurableFunction.GetDeletedChangeFeedInstanceBatchesAsync),
                _options.RetryOptions,
               Arg.Any<CleanupDeletedBatchArguments>())
            .Returns(new List<ChangeFeedEntry>() { });

        context
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                _options.RetryOptions,
                Arg.Is(GetPredicate()))
            .Returns(new DurableOrchestrationStatus { CreatedTime = createdTime });

        // Invoke the orchestration
        await _function.CleanupDeletedFilesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<BlobMigrationCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<ChangeFeedEntry>>(
                nameof(CleanupDeletedDurableFunction.GetDeletedChangeFeedInstanceBatchesAsync),
                _options.RetryOptions,
               Arg.Any<CleanupDeletedBatchArguments>());
        await context
            .Received(0)
            .CallActivityWithRetryAsync<long>(
                nameof(CleanupDeletedDurableFunction.GetMaxDeletedChangeFeedWatermarkAsync),
                _options.RetryOptions,
               Arg.Any<CleanupDeletedBatchArguments>());

        await context
            .Received(0)
            .CallActivityWithRetryAsync(
                nameof(CleanupDeletedDurableFunction.CleanupDeletedBatchAsync),
                _options.RetryOptions,
                Arg.Any<IEnumerable<VersionedInstanceIdentifier>>());

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

    private static Expression<Predicate<GetInstanceStatusOptions>> GetPredicate()
    {
        return x => !x.ShowHistory && !x.ShowHistoryOutput && !x.ShowInput;
    }
}
