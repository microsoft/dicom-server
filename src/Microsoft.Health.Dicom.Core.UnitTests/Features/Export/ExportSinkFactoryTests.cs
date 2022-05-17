// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    public async Task GivenNoProviders_WhenCreatingSink_ThenThrowException()
    {
        var factory = new ExportSinkFactory(Substitute.For<IServiceProvider>(), Array.Empty<IExportSinkProvider>());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => factory.CreateAsync(
            new TypedConfiguration<ExportDestinationType> { Type = ExportDestinationType.AzureBlob },
            Guid.NewGuid()));
    }

    [Fact]
    public async Task GivenValidProviders_WhenCreatingSink_ThenReturnSink()
    {
        using var tokenSource = new CancellationTokenSource();

        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        IConfiguration config = Substitute.For<IConfiguration>();
        var destination = new TypedConfiguration<ExportDestinationType> { Type = ExportDestinationType.AzureBlob, Configuration = config };
        var operationId = Guid.NewGuid();
        IExportSink expected = Substitute.For<IExportSink>();

        IExportSinkProvider provider = Substitute.For<IExportSinkProvider>();
        provider.Type.Returns(ExportDestinationType.AzureBlob);
        provider.CreateAsync(serviceProvider, config, operationId, tokenSource.Token).Returns(expected);

        var factory = new ExportSinkFactory(serviceProvider, new IExportSinkProvider[] { provider });
        Assert.Same(expected, await factory.CreateAsync(destination, operationId, tokenSource.Token));

        await provider.Received(1).CreateAsync(serviceProvider, config, operationId, tokenSource.Token);
    }

    [Fact]
    public async Task GivenValidProviders_WhenSecuring_ThenInvokeCorrectMethod()
    {
        using var tokenSource = new CancellationTokenSource();

        var operationId = Guid.NewGuid();
        IConfiguration config = Substitute.For<IConfiguration>();
        var destination = new TypedConfiguration<ExportDestinationType> { Type = ExportDestinationType.AzureBlob, Configuration = config };
        IConfiguration expectedConfig = Substitute.For<IConfiguration>();

        IExportSinkProvider provider = Substitute.For<IExportSinkProvider>();
        provider.Type.Returns(ExportDestinationType.AzureBlob);
        provider.SecureSensitiveInfoAsync(config, operationId, tokenSource.Token).Returns(expectedConfig);

        var factory = new ExportSinkFactory(Substitute.For<IServiceProvider>(), new IExportSinkProvider[] { provider });
        TypedConfiguration<ExportDestinationType> actual = await factory.SecureSensitiveInfoAsync(destination, operationId, tokenSource.Token);

        await provider.Received(1).SecureSensitiveInfoAsync(config, operationId, tokenSource.Token);
        Assert.Equal(ExportDestinationType.AzureBlob, actual.Type);
        Assert.Same(expectedConfig, actual.Configuration);
    }

    [Fact]
    public async Task GivenNoProviders_WhenValidating_ThenThrowException()
    {
        var factory = new ExportSinkFactory(Substitute.For<IServiceProvider>(), Array.Empty<IExportSinkProvider>());
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => factory.ValidateAsync(
                new TypedConfiguration<ExportDestinationType> { Type = ExportDestinationType.AzureBlob }));
    }

    [Fact]
    public async Task GivenValidProviders_WhenValidating_ThenInvokeCorrectMethod()
    {
        using var tokenSource = new CancellationTokenSource();

        IConfiguration config = Substitute.For<IConfiguration>();
        var destination = new TypedConfiguration<ExportDestinationType> { Type = ExportDestinationType.AzureBlob, Configuration = config };
        IConfiguration expectedConfig = Substitute.For<IConfiguration>();

        IExportSinkProvider provider = Substitute.For<IExportSinkProvider>();
        provider.Type.Returns(ExportDestinationType.AzureBlob);

        var factory = new ExportSinkFactory(Substitute.For<IServiceProvider>(), new IExportSinkProvider[] { provider });
        await factory.ValidateAsync(destination, tokenSource.Token);

        await provider.Received(1).ValidateAsync(config, tokenSource.Token);
    }
}
