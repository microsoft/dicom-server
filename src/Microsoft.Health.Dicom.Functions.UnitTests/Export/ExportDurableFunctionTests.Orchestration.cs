// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Models.Export;
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
            Destination = new ExportDataOptions<ExportDestinationType>(DestinationType, new AzureBlobExportOptions()),
            ErrorHref = new Uri($"http://storage/errors/{operationId}.json"),
            Partition = Partition.Default,
            Source = new ExportDataOptions<ExportSourceType>(SourceType, new IdentifierExportOptions()),
        };
        var batches = new ExportDataOptions<ExportSourceType>[]
        {
            new ExportDataOptions<ExportSourceType>(SourceType, new IdentifierExportOptions()),
            new ExportDataOptions<ExportSourceType>(SourceType, new IdentifierExportOptions()),
        };
        var results = new ExportProgress[]
        {
            new ExportProgress(2, 1),
            new ExportProgress(3, 0),
        };
        var nextSource = new ExportDataOptions<ExportSourceType>(SourceType, new IdentifierExportOptions());

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext(operationId);

        IExportSource source = Substitute.For<IExportSource>();
        source
            .TryDequeueBatch(3, out Arg.Any<ExportDataOptions<ExportSourceType>>())
            .Returns(
                x => { x[1] = batches[0]; return true; },
                x => { x[1] = batches[1]; return true; });
        source.Description.Returns(nextSource);
        _sourceProvider.CreateAsync(input.Source.Settings, input.Partition).Returns(source);

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

        // Invoke the orchestration
        await _function.ExportDicomFilesAsync(context, NullLogger.Instance);

        // Assert behavior
        context
            .Received(1)
            .GetInput<ExportCheckpoint>();
        await _sourceProvider.Received(1).CreateAsync(input.Source.Settings, input.Partition);
        source
            .Received(2)
            .TryDequeueBatch(3, out Arg.Any<ExportDataOptions<ExportSourceType>>());
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
            .DidNotReceive()
            .CallActivityWithRetryAsync(
                nameof(ExportDurableFunction.CompleteCopyAsync),
                _options.RetryOptions,
                Arg.Any<ExportDataOptions<ExportDestinationType>>());
        context
            .Received(1)
            .ContinueAsNew(
                Arg.Is<ExportCheckpoint>(x => ReferenceEquals(x.Source, nextSource)
                    && x.ErrorHref == input.ErrorHref
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
            Destination = new ExportDataOptions<ExportDestinationType>(DestinationType, new AzureBlobExportOptions()),
            ErrorHref = new Uri($"http://storage/errors/{operationId}.json"),
            Partition = Partition.Default,
            Progress = new ExportProgress(1234, 56),
            Source = new ExportDataOptions<ExportSourceType>(SourceType, new IdentifierExportOptions()),
        };
        var batch = new ExportDataOptions<ExportSourceType>(SourceType, new IdentifierExportOptions());
        var newProgress = new ExportProgress(2, 0);

        // Arrange the input
        IDurableOrchestrationContext context = CreateContext(operationId);

        IExportSource source = Substitute.For<IExportSource>();
        source
            .TryDequeueBatch(3, out Arg.Any<ExportDataOptions<ExportSourceType>>())
            .Returns(
                x => { x[1] = batch; return true; },
                x => { x[1] = null; return false; });
        source.Description.Returns((ExportDataOptions<ExportSourceType>)null);
        _sourceProvider.CreateAsync(checkpoint.Source.Settings, checkpoint.Partition).Returns(source);

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
        await _sourceProvider.Received(1).CreateAsync(checkpoint.Source.Settings, checkpoint.Partition);
        source
            .Received(2)
            .TryDequeueBatch(3, out Arg.Any<ExportDataOptions<ExportSourceType>>());
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
            .CallActivityWithRetryAsync(
                nameof(ExportDurableFunction.CompleteCopyAsync),
                _options.RetryOptions,
                Arg.Any<ExportDataOptions<ExportDestinationType>>());
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
            Destination = new ExportDataOptions<ExportDestinationType>(DestinationType, new AzureBlobExportOptions()),
            ErrorHref = new Uri($"http://storage/errors/{operationId}.json"),
            Partition = Partition.Default,
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
        await _sourceProvider.DidNotReceiveWithAnyArgs().CreateAsync(default, default, default);
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
            .Received(1)
            .CallActivityWithRetryAsync(
                nameof(ExportDurableFunction.CompleteCopyAsync),
                _options.RetryOptions,
                checkpoint.Destination);
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
