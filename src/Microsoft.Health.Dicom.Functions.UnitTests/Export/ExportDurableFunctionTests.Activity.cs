// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Models.Export;
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
            ReadResult.ForIdentifier(new VersionedInstanceIdentifier("1", "2", "3", 100)),
            ReadResult.ForIdentifier(new VersionedInstanceIdentifier("4", "5", "6", 101)),
            ReadResult.ForFailure(new ReadFailureEventArgs(DicomIdentifier.ForSeries("7", "8"), new IOException())),
            ReadResult.ForIdentifier(new VersionedInstanceIdentifier("9", "1.0", "1.1", 102)),
        };
        var expectedInput = new ExportBatchArguments
        {
            Destination = new TypedConfiguration<ExportDestinationType> { Type = DestinationType, Configuration = Substitute.For<IConfiguration>() },
            Source = new TypedConfiguration<ExportSourceType> { Type = SourceType, Configuration = Substitute.For<IConfiguration>() },
        };

        // Arrange input, source, and sink
        _options.BatchThreadCount = 2;

        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.InstanceId.Returns(operationId.ToString(OperationId.FormatSpecifier));
        context.GetInput<ExportBatchArguments>().Returns(expectedInput);

        IExportSource source = Substitute.For<IExportSource>();
        source.GetAsyncEnumerator(default).Returns(expectedData.ToAsyncEnumerable().GetAsyncEnumerator());
        _sourceProvider.Create(_serviceProvider, expectedInput.Source.Configuration).Returns(source);

        IExportSink sink = Substitute.For<IExportSink>();
        sink.CopyAsync(expectedData[0]).Returns(true);
        sink.CopyAsync(expectedData[1]).Returns(false);
        sink.CopyAsync(expectedData[2]).Returns(false);
        sink.CopyAsync(expectedData[3]).Returns(true);
        _sinkProvider.Create(_serviceProvider, expectedInput.Destination.Configuration, operationId).Returns(sink);

        // Call the activity
        ExportProgress actual = await _function.ExportBatchAsync(context, NullLogger.Instance);

        // Assert behavior
        Assert.Equal(new ExportProgress(2, 2), actual);

        context.Received(1).GetInput<ExportBatchArguments>();
        _sourceProvider.Received(1).Create(_serviceProvider, expectedInput.Source.Configuration);
        _sinkProvider.Received(1).Create(_serviceProvider, expectedInput.Destination.Configuration, operationId);
        source.Received(1).GetAsyncEnumerator(default);
        await sink.Received(1).CopyAsync(expectedData[0]);
        await sink.Received(1).CopyAsync(expectedData[1]);
        await sink.Received(1).CopyAsync(expectedData[2]);
        await sink.Received(1).CopyAsync(expectedData[3]);
    }

    [Fact]
    public async Task GivenSink_WhenFetchingErrorHref_ThenShouldFetchUri()
    {
        var operationId = Guid.NewGuid();
        var expectedUri = new Uri($"http://storage/errors/{operationId}.json");
        var expectedInput = new TypedConfiguration<ExportDestinationType> { Type = DestinationType, Configuration = Substitute.For<IConfiguration>() };

        // Arrange input, source, and sink
        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.InstanceId.Returns(operationId.ToString(OperationId.FormatSpecifier));
        context.GetInput<TypedConfiguration<ExportDestinationType>>().Returns(expectedInput);

        IExportSink sink = Substitute.For<IExportSink>();
        sink.ErrorHref.Returns(expectedUri);
        _sinkProvider.Create(_serviceProvider, expectedInput.Configuration, operationId).Returns(sink);

        // Call the activity
        Uri actual = await _function.GetErrorHrefAsync(context);

        // Assert behavior
        Assert.Equal(expectedUri, actual);

        context.Received(1).GetInput<TypedConfiguration<ExportDestinationType>>();
        _sinkProvider.Received(1).Create(_serviceProvider, expectedInput.Configuration, operationId);
    }
}
