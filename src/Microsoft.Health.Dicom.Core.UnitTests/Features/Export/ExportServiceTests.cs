// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Models.Export;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Export;

public class ExportServiceTests
{
    private const ExportSourceType SourceType = ExportSourceType.Identifiers;
    private const ExportDestinationType DestinationType = ExportDestinationType.AzureBlob;

    private readonly IExportSourceProvider _sourceProvider;
    private readonly IExportSinkProvider _sinkProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly IGuidFactory _guidFactory;
    private readonly IDicomOperationsClient _client;
    private readonly ExportService _service;

    public ExportServiceTests()
    {
        _sourceProvider = Substitute.For<IExportSourceProvider>();
        _sinkProvider = Substitute.For<IExportSinkProvider>();
        _client = Substitute.For<IDicomOperationsClient>();
        _guidFactory = Substitute.For<IGuidFactory>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _service = new ExportService(
            new ExportSourceFactory(_serviceProvider, new IExportSourceProvider[] { _sourceProvider }),
            new ExportSinkFactory(_serviceProvider, new IExportSinkProvider[] { _sinkProvider }),
            _guidFactory,
            _client);
    }

    [Fact]
    public void GivenNullArgument_WhenConstructing_ThenThrowArgumentNullException()
    {
        var source = new ExportSourceFactory(_serviceProvider, new IExportSourceProvider[] { _sourceProvider });
        var sink = new ExportSinkFactory(_serviceProvider, new IExportSinkProvider[] { _sinkProvider });

        Assert.Throws<ArgumentNullException>(() => new ExportService(null, sink, _guidFactory, _client));
        Assert.Throws<ArgumentNullException>(() => new ExportService(source, null, _guidFactory, _client));
        Assert.Throws<ArgumentNullException>(() => new ExportService(source, sink, null, _client));
        Assert.Throws<ArgumentNullException>(() => new ExportService(source, sink, _guidFactory, null));
    }
}
