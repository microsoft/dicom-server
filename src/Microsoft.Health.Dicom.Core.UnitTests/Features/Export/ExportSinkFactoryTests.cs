// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Export;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Export;

public class ExportSinkFactoryTests
{
    [Fact]
    public void GivenNoProviders_WhenCreatingSink_ThenThrowException()
    {
        var factory = new ExportSinkFactory(Substitute.For<IServiceProvider>(), Array.Empty<IExportSinkProvider>());
        Assert.Throws<KeyNotFoundException>(() => factory.CreateSink(
            new TypedConfiguration<ExportDestinationType> { Type = ExportDestinationType.AzureBlob },
            Guid.NewGuid()));
    }

    [Fact]
    public void GivenValidProviders_WhenCreatingSink_ThenReturnSink()
    {
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        IConfiguration config = Substitute.For<IConfiguration>();
        var source = new TypedConfiguration<ExportDestinationType> { Type = ExportDestinationType.AzureBlob, Configuration = config };
        var operationId = Guid.NewGuid();
        IExportSink expected = Substitute.For<IExportSink>();

        IExportSinkProvider provider = Substitute.For<IExportSinkProvider>();
        provider.Type.Returns(ExportDestinationType.AzureBlob);
        provider.Create(serviceProvider, config, operationId).Returns(expected);

        var factory = new ExportSinkFactory(serviceProvider, new IExportSinkProvider[] { provider });
        Assert.Same(expected, factory.CreateSink(source, operationId));

        provider.Received(1).Create(serviceProvider, config, operationId);
    }

    [Fact]
    public void GivenNoProviders_WhenValidating_ThenThrowException()
    {
        var factory = new ExportSinkFactory(Substitute.For<IServiceProvider>(), Array.Empty<IExportSinkProvider>());
        Assert.Throws<KeyNotFoundException>(() => factory.Validate(new TypedConfiguration<ExportDestinationType> { Type = ExportDestinationType.AzureBlob }));
    }

    [Fact]
    public void GivenValidProviders_WhenValidate_ThenInvokeCorrectMethod()
    {
        IConfiguration config = Substitute.For<IConfiguration>();
        var source = new TypedConfiguration<ExportDestinationType> { Type = ExportDestinationType.AzureBlob, Configuration = config };

        IExportSinkProvider provider = Substitute.For<IExportSinkProvider>();
        provider.Type.Returns(ExportDestinationType.AzureBlob);

        var factory = new ExportSinkFactory(Substitute.For<IServiceProvider>(), new IExportSinkProvider[] { provider });
        factory.Validate(source);

        provider.Received(1).Validate(config);
    }
}
