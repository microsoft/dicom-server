// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Export;

public class ExportServiceTests
{
    private const ExportSourceType SourceType = ExportSourceType.Identifiers;
    private const ExportDestinationType DestinationType = ExportDestinationType.AzureBlob;

    private readonly PartitionEntry _partition = new PartitionEntry(123, "export-partition");
    private readonly IExportSourceProvider _sourceProvider;
    private readonly IExportSinkProvider _sinkProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly IGuidFactory _guidFactory;
    private readonly IDicomOperationsClient _client;
    private readonly IDicomRequestContext _requestContext;
    private readonly ExportService _service;

    public ExportServiceTests()
    {
        _sourceProvider = Substitute.For<IExportSourceProvider>();
        _sourceProvider.Type.Returns(SourceType);
        _sinkProvider = Substitute.For<IExportSinkProvider>();
        _sinkProvider.Type.Returns(DestinationType);
        _client = Substitute.For<IDicomOperationsClient>();
        _guidFactory = Substitute.For<IGuidFactory>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _requestContext = Substitute.For<IDicomRequestContext>();
        _requestContext.DataPartitionEntry.Returns(_partition);
        _service = new ExportService(
            new ExportSourceFactory(_serviceProvider, new IExportSourceProvider[] { _sourceProvider }),
            new ExportSinkFactory(_serviceProvider, new IExportSinkProvider[] { _sinkProvider }),
            _guidFactory,
            _client,
            _requestContext);
    }

    [Fact]
    public void GivenNullArgument_WhenConstructing_ThenThrowArgumentNullException()
    {
        var source = new ExportSourceFactory(_serviceProvider, new IExportSourceProvider[] { _sourceProvider });
        var sink = new ExportSinkFactory(_serviceProvider, new IExportSinkProvider[] { _sinkProvider });

        Assert.Throws<ArgumentNullException>(() => new ExportService(null, sink, _guidFactory, _client, _requestContext));
        Assert.Throws<ArgumentNullException>(() => new ExportService(source, null, _guidFactory, _client, _requestContext));
        Assert.Throws<ArgumentNullException>(() => new ExportService(source, sink, null, _client, _requestContext));
        Assert.Throws<ArgumentNullException>(() => new ExportService(source, sink, _guidFactory, null, _requestContext));
        Assert.Throws<ArgumentNullException>(() => new ExportService(source, sink, _guidFactory, _client, null));
    }

    [Fact]
    public async Task GivenSpecification_WhenStartingExport_ThenValidateBeforeStarting()
    {
        using var tokenSource = new CancellationTokenSource();

        var operationId = Guid.NewGuid();
        var originalSource = new object();
        var originalDestination = new object();
        var newDestination = new object();
        var spec = new ExportSpecification
        {
            Destination = new ExportDataOptions<ExportDestinationType>(DestinationType, originalDestination),
            Source = new ExportDataOptions<ExportSourceType>(SourceType, originalSource),
        };
        var expected = new OperationReference(operationId, new Uri("http://test/export"));

        _guidFactory.Create().Returns(operationId);
        _sinkProvider.SecureSensitiveInfoAsync(originalDestination, operationId, tokenSource.Token).Returns(newDestination);
        _client
            .StartExportAsync(
                operationId,
                Arg.Is<ExportSpecification>(x => ReferenceEquals(originalSource, x.Source.Settings)
                    && ReferenceEquals(newDestination, x.Destination.Settings)),
                _partition,
                tokenSource.Token)
            .Returns(expected);

        Assert.Same(expected, await _service.StartExportAsync(spec, tokenSource.Token));

        _guidFactory.Received(1).Create();
        await _sourceProvider.Received(1).ValidateAsync(originalSource, tokenSource.Token);
        await _sinkProvider.Received(1).ValidateAsync(originalDestination, tokenSource.Token);
        await _client
            .Received(1)
            .StartExportAsync(
                operationId,
                Arg.Is<ExportSpecification>(x => ReferenceEquals(originalSource, x.Source.Settings)
                    && ReferenceEquals(newDestination, x.Destination.Settings)),
                _partition,
                tokenSource.Token);
    }
}
