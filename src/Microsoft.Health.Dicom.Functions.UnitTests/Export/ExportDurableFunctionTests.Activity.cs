// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Functions.Export;
using Microsoft.Health.Dicom.Functions.Export.Models;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Export;

public partial class ExportDurableFunctionTests
{
    [Fact]
    public async Task GivenBatch_WhenExporting_ThenShouldCopyFiles()
    {
        var operationId = Guid.NewGuid();
        var expectedData = new ReadResult[]
        {
            ReadResult.ForInstance(new InstanceMetadata(new VersionedInstanceIdentifier("1", "2", "3", 100), new InstanceProperties())),
            ReadResult.ForInstance(new InstanceMetadata(new VersionedInstanceIdentifier("4", "5", "6", 101), new InstanceProperties())),
            ReadResult.ForFailure(new ReadFailureEventArgs(DicomIdentifier.ForSeries("7", "8"), new IOException())),
            ReadResult.ForInstance(new InstanceMetadata(new VersionedInstanceIdentifier("9", "1.0", "1.1", 102), new InstanceProperties())),
            ReadResult.ForInstance(new InstanceMetadata(new VersionedInstanceIdentifier("121.3", "14", "1.516", 103), new InstanceProperties()))
        };
        var expectedInput = new ExportBatchArguments
        {
            Destination = new ExportDataOptions<ExportDestinationType>(DestinationType, new AzureBlobExportOptions()),
            Partition = Partition.Default,
            Source = new ExportDataOptions<ExportSourceType>(SourceType, new IdentifierExportOptions()),
        };

        // Arrange input, source, and sink
        _options.MaxParallelThreads = 2;

        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.InstanceId.Returns(operationId.ToString(OperationId.FormatSpecifier));
        context.GetInput<ExportBatchArguments>().Returns(expectedInput);

        // Note: Parallel.ForEachAsync uses its own CancellationTokenSource
        IExportSource source = Substitute.For<IExportSource>();
        source.GetAsyncEnumerator(Arg.Any<CancellationToken>()).Returns(expectedData.ToAsyncEnumerable().GetAsyncEnumerator());
        _sourceProvider.CreateAsync(expectedInput.Source.Settings, expectedInput.Partition).Returns(source);

        IExportSink sink = Substitute.For<IExportSink>();
        sink.CopyAsync(expectedData[0], Arg.Any<CancellationToken>()).Returns(true);
        sink.CopyAsync(expectedData[1], Arg.Any<CancellationToken>()).Returns(false);
        sink.CopyAsync(expectedData[2], Arg.Any<CancellationToken>()).Returns(false);
        sink.CopyAsync(expectedData[3], Arg.Any<CancellationToken>()).Returns(true);
        sink.CopyAsync(expectedData[4], Arg.Any<CancellationToken>()).Returns(true);
        _sinkProvider.CreateAsync(expectedInput.Destination.Settings, operationId).Returns(sink);

        // Call the activity
        ExportProgress actual = await _function.ExportBatchAsync(context, NullLogger.Instance);

        // Assert behavior
        Assert.Equal(new ExportProgress(3, 2), actual);

        context.Received(1).GetInput<ExportBatchArguments>();
        await _sourceProvider.Received(1).CreateAsync(expectedInput.Source.Settings, expectedInput.Partition);
        await _sinkProvider.Received(1).CreateAsync(expectedInput.Destination.Settings, operationId);
        source.Received(1).GetAsyncEnumerator(Arg.Any<CancellationToken>());
        await sink.Received(1).CopyAsync(expectedData[0], Arg.Any<CancellationToken>());
        await sink.Received(1).CopyAsync(expectedData[1], Arg.Any<CancellationToken>());
        await sink.Received(1).CopyAsync(expectedData[2], Arg.Any<CancellationToken>());
        await sink.Received(1).CopyAsync(expectedData[3], Arg.Any<CancellationToken>());
        await sink.Received(1).CopyAsync(expectedData[4], Arg.Any<CancellationToken>());
        await sink.Received(1).FlushAsync(default);
    }

    [Fact]
    public async Task GivenDestination_WhenComplete_ThenInvokeCorrectMethod()
    {
        var expected = new AzureBlobExportOptions();
        await _function.CompleteCopyAsync(new ExportDataOptions<ExportDestinationType>(DestinationType, expected));

        await _sinkProvider.Received(1).CompleteCopyAsync(expected, default);
    }
}
