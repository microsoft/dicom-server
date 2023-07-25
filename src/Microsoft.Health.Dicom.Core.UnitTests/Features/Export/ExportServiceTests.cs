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
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Export;

public class ExportServiceTests
{
    private const ExportSourceType SourceType = ExportSourceType.Identifiers;
    private const ExportDestinationType DestinationType = ExportDestinationType.AzureBlob;

    private readonly Partition _partition = new Partition(123, "export-partition");
    private readonly IExportSink _sink;
    private readonly IExportSourceProvider _sourceProvider;
    private readonly IExportSinkProvider _sinkProvider;
    private readonly IGuidFactory _guidFactory;
    private readonly IDicomOperationsClient _client;
    private readonly IDicomRequestContextAccessor _requestContextAccessor;
    private readonly IDicomRequestContext _requestContext;
    private readonly ExportService _service;

    public ExportServiceTests()
    {
        _sourceProvider = Substitute.For<IExportSourceProvider>();
        _sourceProvider.Type.Returns(SourceType);
        _sink = Substitute.For<IExportSink>();
        _sinkProvider = Substitute.For<IExportSinkProvider>();
        _sinkProvider.Type.Returns(DestinationType);
        _client = Substitute.For<IDicomOperationsClient>();
        _guidFactory = Substitute.For<IGuidFactory>();
        _requestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        _requestContext = Substitute.For<IDicomRequestContext>();
        _requestContext.DataPartition.Returns(_partition);
        _requestContextAccessor.RequestContext.Returns(_requestContext);
        _service = new ExportService(
            new ExportSourceFactory(new IExportSourceProvider[] { _sourceProvider }),
            new ExportSinkFactory(new IExportSinkProvider[] { _sinkProvider }),
            _guidFactory,
            _client,
            _requestContextAccessor);
    }

    [Fact]
    public void GivenNullArgument_WhenConstructing_ThenThrowArgumentNullException()
    {
        var source = new ExportSourceFactory(new IExportSourceProvider[] { _sourceProvider });
        var sink = new ExportSinkFactory(new IExportSinkProvider[] { _sinkProvider });

        Assert.Throws<ArgumentNullException>(() => new ExportService(null, sink, _guidFactory, _client, _requestContextAccessor));
        Assert.Throws<ArgumentNullException>(() => new ExportService(source, null, _guidFactory, _client, _requestContextAccessor));
        Assert.Throws<ArgumentNullException>(() => new ExportService(source, sink, null, _client, _requestContextAccessor));
        Assert.Throws<ArgumentNullException>(() => new ExportService(source, sink, _guidFactory, null, _requestContextAccessor));
        Assert.Throws<ArgumentNullException>(() => new ExportService(source, sink, _guidFactory, _client, null));
    }

    [Fact]
    public async Task GivenSpecification_WhenStartingExport_ThenValidateBeforeStarting()
    {
        using var tokenSource = new CancellationTokenSource();

        var operationId = Guid.NewGuid();
        var sourceSettings = new object();
        var originalDestinationSettings = new object();
        var securedDestinationSettings = new object();
        var spec = new ExportSpecification
        {
            Destination = new ExportDataOptions<ExportDestinationType>(DestinationType, originalDestinationSettings),
            Source = new ExportDataOptions<ExportSourceType>(SourceType, sourceSettings),
        };
        var errorHref = new Uri($"https://somewhere/{operationId:N}/errors.log");
        var expected = new OperationReference(operationId, new Uri("http://test/export"));

        _guidFactory.Create().Returns(operationId);
        _sinkProvider.CreateAsync(originalDestinationSettings, operationId, tokenSource.Token).Returns(_sink);
        _sinkProvider.SecureSensitiveInfoAsync(originalDestinationSettings, operationId, tokenSource.Token).Returns(securedDestinationSettings);
        _sink.InitializeAsync(tokenSource.Token).Returns(errorHref);
        _client
            .StartExportAsync(
                operationId,
                Arg.Is<ExportSpecification>(x => ReferenceEquals(sourceSettings, x.Source.Settings)
                    && ReferenceEquals(securedDestinationSettings, x.Destination.Settings)),
                errorHref,
                _partition,
                tokenSource.Token)
            .Returns(expected);

        Assert.Same(expected, await _service.StartExportAsync(spec, tokenSource.Token));

        _guidFactory.Received(1).Create();
        await _sourceProvider.Received(1).ValidateAsync(sourceSettings, tokenSource.Token);
        await _sinkProvider.Received(1).ValidateAsync(originalDestinationSettings, tokenSource.Token);
        await _sinkProvider.Received(1).CreateAsync(originalDestinationSettings, operationId, tokenSource.Token);
        await _sinkProvider.Received(1).SecureSensitiveInfoAsync(originalDestinationSettings, operationId, tokenSource.Token);
        await _sink.Received(1).InitializeAsync(tokenSource.Token);
        await _client
            .Received(1)
            .StartExportAsync(
                operationId,
                Arg.Is<ExportSpecification>(x => ReferenceEquals(sourceSettings, x.Source.Settings)
                    && ReferenceEquals(securedDestinationSettings, x.Destination.Settings)),
                errorHref,
                _partition,
                tokenSource.Token);

        // Ensure that validation was called before creation
        Received.InOrder(
            () =>
            {
                _sourceProvider.ValidateAsync(sourceSettings, tokenSource.Token);
                _sinkProvider.ValidateAsync(originalDestinationSettings, tokenSource.Token);
                _sinkProvider.CreateAsync(originalDestinationSettings, operationId, tokenSource.Token);
            });
    }
}
