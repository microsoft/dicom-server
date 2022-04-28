// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Export;
using Microsoft.Health.Dicom.Functions.Export.Models;
using Microsoft.Health.Operations;
using Microsoft.Health.Operations.Functions.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Export;

public partial class ExportDurableFunctionTests
{
    [Fact]
    public async Task GivenNewOrchestration_WhenExportingFiles_ThenExportBatches()
    {
        string operationId = OperationId.Generate();
        DateTime createdTime = DateTime.UtcNow;
        var input = new ExportCheckpoint
        {
            Batching = new BatchingOptions
            {
                Size = 3,
                MaxParallelCount = 2,
            },
            Destination = new TypedConfiguration<ExportDestinationType> { Type = DestinationType, Configuration = Substitute.For<IConfiguration>() },
            Partition = PartitionEntry.Default,
            Source = new TypedConfiguration<ExportSourceType> { Type = SourceType, Configuration = Substitute.For<IConfiguration>() },
        };
        var batches = new TypedConfiguration<ExportSourceType>[]
        {
            new TypedConfiguration<ExportSourceType> { Type = SourceType, Configuration = Substitute.For<IConfiguration>() },
            new TypedConfiguration<ExportSourceType> { Type = SourceType, Configuration = Substitute.For<IConfiguration>() },
        };
        var results = new ExportProgress[]
        {
            new ExportProgress(2, 1),
            new ExportProgress(3, 0),
        };
        var nextSource = new TypedConfiguration<ExportSourceType> { Type = SourceType, Configuration = Substitute.For<IConfiguration>() };
        var errorHref = new Uri($"http://storage/errors/{operationId}.json");

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext(operationId);

        IExportSource source = Substitute.For<IExportSource>();
        source
            .TryDequeueBatch(3, out Arg.Any<TypedConfiguration<ExportSourceType>>())
            .Returns(
                x => { x[1] = batches[0]; return true; },
                x => { x[1] = batches[1]; return true; });
        source.Configuration.Returns(nextSource);
        _sourceProvider.Create(_serviceProvider, input.Source.Configuration, input.Partition).Returns(source);

        context
            .GetInput<ExportCheckpoint>()
            .Returns(input);
        context
            .CallActivityWithRetryAsync<ExportProgress>(
                nameof(ExportDurableFunction.ExportBatchAsync),
                _options.RetryOptions,
                Arg.Is<ExportBatchArguments>(x => ReferenceEquals(x.Source, batches[0]) && ReferenceEquals(x.Destination, input.Destination)))
            .Returns(results[0]);
        context
            .CallActivityWithRetryAsync<ExportProgress>(
                nameof(ExportDurableFunction.ExportBatchAsync),
                _options.RetryOptions,
                Arg.Is<ExportBatchArguments>(x => ReferenceEquals(x.Source, batches[1]) && ReferenceEquals(x.Destination, input.Destination)))
            .Returns(results[1]);
        context
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                _options.RetryOptions,
                Arg.Any<GetInstanceStatusOptions>())
            .Returns(new DurableOrchestrationStatus { CreatedTime = createdTime });
        context
            .CallActivityWithRetryAsync<Uri>(
                nameof(ExportDurableFunction.GetErrorHrefAsync),
                _options.RetryOptions,
                input.Destination)
            .Returns(errorHref);

        // Invoke the orchestration
        await _function.ExportDicomFilesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<ExportCheckpoint>();
        _sourceProvider.Received(1).Create(_serviceProvider, input.Source.Configuration, input.Partition);
        source
            .Received(2)
            .TryDequeueBatch(3, out Arg.Any<TypedConfiguration<ExportSourceType>>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync<ExportProgress>(
                nameof(ExportDurableFunction.ExportBatchAsync),
                _options.RetryOptions,
                Arg.Is<ExportBatchArguments>(x => ReferenceEquals(x.Source, batches[0]) && ReferenceEquals(x.Destination, input.Destination)));
        await context
            .Received(1)
            .CallActivityWithRetryAsync<ExportProgress>(
                nameof(ExportDurableFunction.ExportBatchAsync),
                _options.RetryOptions,
                Arg.Is<ExportBatchArguments>(x => ReferenceEquals(x.Source, batches[1]) && ReferenceEquals(x.Destination, input.Destination)));
        await context
            .Received(1)
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                _options.RetryOptions,
                Arg.Any<GetInstanceStatusOptions>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync<Uri>(
                nameof(ExportDurableFunction.GetErrorHrefAsync),
                _options.RetryOptions,
                input.Destination);
        context
            .Received(1)
            .ContinueAsNew(
                Arg.Is<ExportCheckpoint>(x => ReferenceEquals(x.Source, nextSource)
                    && x.ErrorHref == errorHref
                    && x.CreatedTime == createdTime
                    && x.Progress == new ExportProgress(5, 1)),
                false);
    }

    [Fact]
    public async Task GivenExistingOrchestration_WhenExportingFiles_ThenExportBatches()
    {
        string operationId = OperationId.Generate();
        var checkpoint = new ExportCheckpoint
        {
            Batching = new BatchingOptions
            {
                Size = 3,
                MaxParallelCount = 2,
            },
            CreatedTime = DateTime.UtcNow,
            Destination = new TypedConfiguration<ExportDestinationType> { Type = DestinationType, Configuration = Substitute.For<IConfiguration>() },
            ErrorHref = new Uri($"http://storage/errors/{operationId}.json"),
            Partition = PartitionEntry.Default,
            Progress = new ExportProgress(1234, 56),
            Source = new TypedConfiguration<ExportSourceType> { Type = SourceType, Configuration = Substitute.For<IConfiguration>() },
        };
        var batch = new TypedConfiguration<ExportSourceType> { Type = SourceType, Configuration = Substitute.For<IConfiguration>() };
        var newProgress = new ExportProgress(2, 0);

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext(operationId);

        IExportSource source = Substitute.For<IExportSource>();
        source
            .TryDequeueBatch(3, out Arg.Any<TypedConfiguration<ExportSourceType>>())
            .Returns(
                x => { x[1] = batch; return true; },
                x => { x[1] = null; return false; });
        source.Configuration.Returns((TypedConfiguration<ExportSourceType>)null);
        _sourceProvider.Create(_serviceProvider, checkpoint.Source.Configuration, checkpoint.Partition).Returns(source);

        context
            .GetInput<ExportCheckpoint>()
            .Returns(checkpoint);
        context
            .CallActivityWithRetryAsync<ExportProgress>(
                nameof(ExportDurableFunction.ExportBatchAsync),
                _options.RetryOptions,
                Arg.Is<ExportBatchArguments>(x => ReferenceEquals(x.Source, batch) && ReferenceEquals(x.Destination, checkpoint.Destination)))
            .Returns(newProgress);

        // Invoke the orchestration
        await _function.ExportDicomFilesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<ExportCheckpoint>();
        _sourceProvider.Received(1).Create(_serviceProvider, checkpoint.Source.Configuration, checkpoint.Partition);
        source
            .Received(2)
            .TryDequeueBatch(3, out Arg.Any<TypedConfiguration<ExportSourceType>>());
        await context
            .Received(1)
            .CallActivityWithRetryAsync<ExportProgress>(
                nameof(ExportDurableFunction.ExportBatchAsync),
                _options.RetryOptions,
                Arg.Is<ExportBatchArguments>(x => ReferenceEquals(x.Source, batch) && ReferenceEquals(x.Destination, checkpoint.Destination)));
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                Arg.Any<RetryOptions>(),
                Arg.Any<object>());
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<Uri>(
                nameof(ExportDurableFunction.GetErrorHrefAsync),
                Arg.Any<RetryOptions>(),
                Arg.Any<object>());
        context
            .Received(1)
            .ContinueAsNew(
                Arg.Is<ExportCheckpoint>(x => x.Source == null && x.Progress == new ExportProgress(1236, 56)),
                false);
    }

    [Fact]
    public async Task GivenCompletedOrchestration_WhenExportingFiles_ThenFinish()
    {
        string operationId = OperationId.Generate();
        var checkpoint = new ExportCheckpoint
        {
            Batching = new BatchingOptions
            {
                Size = 3,
                MaxParallelCount = 2,
            },
            CreatedTime = DateTime.UtcNow,
            Destination = new TypedConfiguration<ExportDestinationType> { Type = DestinationType, Configuration = Substitute.For<IConfiguration>() },
            ErrorHref = new Uri($"http://storage/errors/{operationId}.json"),
            Partition = PartitionEntry.Default,
            Progress = new ExportProgress(78910, 0),
            Source = null,
        };

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext(operationId);

        context
            .GetInput<ExportCheckpoint>()
            .Returns(checkpoint);

        // Invoke the orchestration
        await _function.ExportDicomFilesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<ExportCheckpoint>();
        _sourceProvider.DidNotReceiveWithAnyArgs().Create(default, default, default);
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<ExportProgress>(
                nameof(ExportDurableFunction.ExportBatchAsync),
                Arg.Any<RetryOptions>(),
                Arg.Any<object>());
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                Arg.Any<RetryOptions>(),
                Arg.Any<object>());
        await context
            .DidNotReceive()
            .CallActivityWithRetryAsync<Uri>(
                nameof(ExportDurableFunction.GetErrorHrefAsync),
                Arg.Any<RetryOptions>(),
                Arg.Any<object>());
        context
            .DidNotReceiveWithAnyArgs()
            .ContinueAsNew(default, default);
    }

    private static IDurableOrchestrationContext CreateContext(string operationId)
    {
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(operationId);
        return context;
    }
}
