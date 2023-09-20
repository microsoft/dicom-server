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
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Functions.Update;
using Microsoft.Health.Dicom.Functions.Update.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Operations;
using Microsoft.IdentityModel.Tokens;
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

        List<InstanceMetadata> instanceMetadataList = CreateExpectedInstanceMetadataList(expectedInstancesWithNewWatermark);

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);

        context
            .GetInput<UpdateCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkV2Async),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArgumentsV2>())
            .Returns(instanceMetadataList);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(expectedInput.Partition, instanceMetadataList, expectedInput.ChangeDataset))
            )
            .Returns(instanceMetadataList);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyV2Async),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArgumentsV2>())
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.DeleteOldVersionBlobV2Async),
                _options.RetryOptions,
                Arg.Any<CleanupBlobArguments>())
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.SetOriginalBlobToColdAccessTierAsync),
                _options.RetryOptions,
                Arg.Any<CleanupBlobArguments>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _updateDurableFunction.UpdateInstancesV2Async(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<UpdateCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkV2Async),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArgumentsV2>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(expectedInput.Partition, instanceMetadataList, expectedInput.ChangeDataset))
                );
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyV2Async),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArgumentsV2>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.DeleteOldVersionBlobV2Async),
                _options.RetryOptions,
                Arg.Any<CleanupBlobArguments>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.SetOriginalBlobToColdAccessTierAsync),
                _options.RetryOptions,
                Arg.Any<CleanupBlobArguments>());
        context
            .Received(1)
            .ContinueAsNew(
                Arg.Is<UpdateCheckpoint>(x => x.NumberOfStudyCompleted == 1),
                false);
    }

    [Fact]
    public async Task GivenNewOrchestrationWithInputAndExternalStoreEnabled_WhenUpdatingInstances_ThenInstanceMetadataListWithFilePropertiesPassedInToCompleteUpdate()
    {
        const int batchSize = 5;
        _options.BatchSize = batchSize;

        var expectedInput = GetUpdateCheckpoint();
        var studyInstanceUid = expectedInput.StudyInstanceUids[expectedInput.NumberOfStudyCompleted];

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

        List<InstanceMetadata> instanceMetadataList = CreateExpectedInstanceMetadataList(expectedInstancesWithNewWatermark, studyInstanceUid);

        context
            .GetInput<UpdateCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkV2Async),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArgumentsV2>())
            .Returns(instanceMetadataList);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(expectedInput.Partition, instanceMetadataList, expectedInput.ChangeDataset))
            )
            .Returns(instanceMetadataList);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(expectedInput.Partition.Key, studyInstanceUid, expectedInput.ChangeDataset, instanceMetadataList)))
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.DeleteOldVersionBlobV2Async),
                _options.RetryOptions,
                expectedInstances)
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.SetOriginalBlobToColdAccessTierAsync),
                _options.RetryOptions,
                Arg.Any<CleanupBlobArguments>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _updateDurableFunctionWithExternalStore.UpdateInstancesV2Async(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<UpdateCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkV2Async),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArgumentsV2>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(expectedInput.Partition, instanceMetadataList, expectedInput.ChangeDataset))
                );
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(expectedInput.Partition.Key, studyInstanceUid, expectedInput.ChangeDataset, instanceMetadataList)));
        context
            .Received(1)
            .ContinueAsNew(
                Arg.Is<UpdateCheckpoint>(x => x.NumberOfStudyCompleted == 1),
                false);
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.DeleteOldVersionBlobV2Async),
                _options.RetryOptions,
                Arg.Any<CleanupBlobArguments>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.SetOriginalBlobToColdAccessTierAsync),
                _options.RetryOptions,
                Arg.Any<CleanupBlobArguments>());
    }

    private static List<InstanceMetadata> CreateExpectedInstanceMetadataList(List<InstanceFileState> expectedInstancesWithNewWatermark, string studyInstanceUid = "0")
    {
        List<InstanceMetadata> instanceMetadataList = expectedInstancesWithNewWatermark.Select(x => new InstanceMetadata(new VersionedInstanceIdentifier(studyInstanceUid, "0", "0", x.Version), new InstanceProperties
        {
            fileProperties = new FileProperties
            {
                ETag = $"etag-{x.NewVersion.ToString()}",
                Path = $"path-{x.NewVersion.ToString()}",
            }
        })).ToList();
        return instanceMetadataList;
    }

    [Fact]
    public async Task GivenNewOrchestrationWithInputAndExternalStoreNotEnabled_WhenUpdatingInstances_ThenEmptyInstanceMetadataListPassedInToCompleteUpdate()
    {
        const int batchSize = 5;
        _options.BatchSize = batchSize;

        var expectedInput = GetUpdateCheckpoint();
        var studyInstanceUid = expectedInput.StudyInstanceUids[expectedInput.NumberOfStudyCompleted];

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

        List<InstanceMetadata> instanceMetadataList = CreateExpectedInstanceMetadataList(expectedInstancesWithNewWatermark);

        context
            .GetInput<UpdateCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkV2Async),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArgumentsV2>())
            .Returns(instanceMetadataList);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(expectedInput.Partition, instanceMetadataList, expectedInput.ChangeDataset))
            )
            .Returns(instanceMetadataList);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(expectedInput.Partition.Key, studyInstanceUid, expectedInput.ChangeDataset, new List<InstanceMetadata>())))
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.DeleteOldVersionBlobV2Async),
                _options.RetryOptions,
                expectedInstances)
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.SetOriginalBlobToColdAccessTierAsync),
                _options.RetryOptions,
                Arg.Any<CleanupBlobArguments>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _updateDurableFunction.UpdateInstancesV2Async(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<UpdateCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkV2Async),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArgumentsV2>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(expectedInput.Partition, instanceMetadataList, expectedInput.ChangeDataset))
                );
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(expectedInput.Partition.Key, studyInstanceUid, expectedInput.ChangeDataset, null, expectEmptyList: true)));
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
            Partition = Partition.Default,
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

        List<InstanceMetadata> instanceMetadataList = CreateExpectedInstanceMetadataList(expectedInstancesWithNewWatermark);

        context
            .GetInput<UpdateCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkV2Async),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArgumentsV2>())
            .Returns(instanceMetadataList);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(Partition.Default, instanceMetadataList, expectedInput.ChangeDataset)))
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyV2Async),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArgumentsV2>())
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.DeleteOldVersionBlobV2Async),
                _options.RetryOptions,
                expectedInstances)
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.SetOriginalBlobToColdAccessTierAsync),
                _options.RetryOptions,
                Arg.Any<CleanupBlobArguments>())
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _updateDurableFunction.UpdateInstancesV2Async(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<UpdateCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkV2Async),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArgumentsV2>());
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsV2Async),
                _options.RetryOptions,
               Arg.Is(GetPredicate(Partition.Default, instanceMetadataList, expectedInput.ChangeDataset)));
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyV2Async),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArgumentsV2>());
        context
            .Received(1)
            .ContinueAsNew(
                Arg.Any<UpdateCheckpoint>(),
                false);
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.DeleteOldVersionBlobV2Async),
                _options.RetryOptions,
                Arg.Any<CleanupBlobArguments>());
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.SetOriginalBlobToColdAccessTierAsync),
                _options.RetryOptions,
                Arg.Any<CleanupBlobArguments>());

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
            Partition = Partition.Default,
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
        await Assert.ThrowsAsync<OperationErrorException>(() => _updateDurableFunction.UpdateInstancesV2Async(context, NullLogger.Instance));

        // Assert behavior
        context
            .Received(1)
            .GetInput<UpdateCheckpoint>();
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkV2Async),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArgumentsV2>());
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsV2Async),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceBlobArgumentsV2>());
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyV2Async),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArgumentsV2>());
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

        List<InstanceMetadata> instanceMetadataList = new List<InstanceMetadata> {
            new InstanceMetadata(
                new VersionedInstanceIdentifier(
                    TestUidGenerator.Generate(),
                    TestUidGenerator.Generate(),
                    TestUidGenerator.Generate(),
                    version: 1,
                    Partition.Default),
                new InstanceProperties
                {
                    fileProperties = new FileProperties { ETag = $"etag-{1}", Path = $"path-{1}" },
                    NewVersion = 3
                }
            ),
            new InstanceMetadata(
                new VersionedInstanceIdentifier(
                    TestUidGenerator.Generate(),
                    TestUidGenerator.Generate(),
                    TestUidGenerator.Generate(),
                    version: 2,
                    Partition.Default),
                new InstanceProperties
                {
                    fileProperties = new FileProperties { ETag = $"etag-{2}", Path = $"path-{2}" },
                    NewVersion = 4
                }
            )
            };

        var expectedInstancesWithNewWatermark = instanceMetadataList.Select(x => x.ToInstanceFileState()).ToList();

        var expectedInput = new UpdateCheckpoint
        {
            Partition = Partition.Default,
            ChangeDataset = string.Empty,
            StudyInstanceUids = instanceMetadataList.Select(x => x.VersionedInstanceIdentifier.StudyInstanceUid).ToList(),
            CreatedTime = createdTime,
        };

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);

        context
            .GetInput<UpdateCheckpoint>()
            .Returns(expectedInput);

        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkV2Async),
                _options.RetryOptions, Arg.Any<UpdateInstanceWatermarkArgumentsV2>()).Returns(instanceMetadataList);

        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(Partition.Default, instanceMetadataList, expectedInput.ChangeDataset)))
            .ThrowsAsync(new FunctionFailedException("Function failed"));

        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CleanupNewVersionBlobV2Async),
                _options.RetryOptions,
                expectedInstancesWithNewWatermark)
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _updateDurableFunction.UpdateInstancesV2Async(context, NullLogger.Instance);

        // Assert behavior
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CleanupNewVersionBlobV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(expectedInstancesWithNewWatermark, Partition.Default)));

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

        List<InstanceMetadata> instanceMetadataList = CreateExpectedInstanceMetadataList(expectedInstancesWithNewWatermark);

        // Arrange the input
        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = CreateContext(operationId);

        context
            .GetInput<UpdateCheckpoint>()
            .Returns(expectedInput);
        context
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkV2Async),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArgumentsV2>())
            .Returns(instanceMetadataList);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsV2Async),
                _options.RetryOptions,
                Arg.Is(GetPredicate(Partition.Default, instanceMetadataList, expectedInput.ChangeDataset)))
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyV2Async),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArgumentsV2>())
            .Returns(Task.CompletedTask);
        context
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.DeleteOldVersionBlobV2Async),
                _options.RetryOptions,
                expectedInstances)
            .Returns(Task.CompletedTask);

        // Invoke the orchestration
        await _updateDurableFunction.UpdateInstancesV2Async(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<UpdateCheckpoint>();
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceWatermarkV2Async),
                _options.RetryOptions,
                Arg.Any<UpdateInstanceWatermarkArgumentsV2>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                nameof(UpdateDurableFunction.UpdateInstanceBlobsV2Async),
                _options.RetryOptions,
               Arg.Is(GetPredicate(Partition.Default, instanceMetadataList, expectedInput.ChangeDataset)));
        await context
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(UpdateDurableFunction.CompleteUpdateStudyV2Async),
                _options.RetryOptions,
                Arg.Any<CompleteStudyArgumentsV2>());
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
            Partition = Partition.Default,
            ChangeDataset = string.Empty,
            StudyInstanceUids = new List<string> {
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate()
            },
            CreatedTime = DateTime.UtcNow
        };

    private static IDurableOrchestrationContext CreateContext(string operationId)
    {
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(operationId);
        return context;
    }

    private static Expression<Predicate<UpdateInstanceBlobArgumentsV2>> GetPredicate(Partition partition, IReadOnlyList<InstanceMetadata> instanceMetadataList, string changeDataset)
    {
        return x =>
            x.InstanceMetadataList == instanceMetadataList
            && x.ChangeDataset == changeDataset
            && x.Partition == partition;
    }

    private static Expression<Predicate<InstanceMetadata>> GetPredicate(InstanceMetadata instance)
    {
        return x =>
            x.InstanceProperties.NewVersion == instance.InstanceProperties.NewVersion
            && x.VersionedInstanceIdentifier.Version == instance.VersionedInstanceIdentifier.Version
            && x.InstanceProperties.OriginalVersion == instance.InstanceProperties.OriginalVersion;
    }

    private static Expression<Predicate<CompleteStudyArgumentsV2>> GetPredicate(int partitionKey, string studyInstanceUid, string dicomDataset, IReadOnlyList<InstanceMetadata> instanceMetadataList, bool expectEmptyList = false)
    {
        return x =>
            x.PartitionKey == partitionKey
            && x.StudyInstanceUid == studyInstanceUid
            && x.ChangeDataset == dicomDataset
            && expectEmptyList ? x.InstanceMetadataList.IsNullOrEmpty() : x.InstanceMetadataList == instanceMetadataList;
    }

    private static Expression<Predicate<CleanupBlobArguments>> GetPredicate(IReadOnlyList<InstanceFileState> instanceWatermarks, Partition partition)
    {
        return x => x.InstanceWatermarks.IsNullOrEmpty() == false
            && x.InstanceWatermarks[0].Version == instanceWatermarks[0].Version
            && x.InstanceWatermarks[1].Version == instanceWatermarks[1].Version
            && x.Partition == partition;
    }
    private static Expression<Predicate<UpdateCheckpoint>> GetPredicate(long instanceUpdated, int studyCompleted)
    {
        return r => r.TotalNumberOfInstanceUpdated == instanceUpdated
        && r.NumberOfStudyCompleted == studyCompleted;
    }
}
