// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models.Export;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Export;

public class ExportSinkFactoryTests
{
    [Fact]
    public async Task GivenNoProviders_WhenCompletingCopy_ThenThrowException()
    {
        var factory = new ExportSinkFactory(Array.Empty<IExportSinkProvider>());
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => factory.CompleteCopyAsync(new ExportDataOptions<ExportDestinationType>(ExportDestinationType.AzureBlob)));
    }

    [Fact]
    public async Task GivenValidProviders_WhenCompletingCopy_ThenInvokeCorrectMethod()
    {
        using var tokenSource = new CancellationTokenSource();

        var options = new AzureBlobExportOptions();
        var destination = new ExportDataOptions<ExportDestinationType>(ExportDestinationType.AzureBlob, options);

        IExportSinkProvider provider = Substitute.For<IExportSinkProvider>();
        provider.Type.Returns(ExportDestinationType.AzureBlob);

        var factory = new ExportSinkFactory(new IExportSinkProvider[] { provider });
        await factory.CompleteCopyAsync(destination, tokenSource.Token);

        await provider.Received(1).CompleteCopyAsync(options, tokenSource.Token);
    }

    [Fact]
    public async Task GivenNoProviders_WhenCreatingSink_ThenThrowException()
    {
        var factory = new ExportSinkFactory(Array.Empty<IExportSinkProvider>());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => factory.CreateAsync(
            new ExportDataOptions<ExportDestinationType>(ExportDestinationType.AzureBlob),
            Guid.NewGuid()));
    }

    [Fact]
    public async Task GivenValidProviders_WhenCreatingSink_ThenReturnSink()
    {
        using var tokenSource = new CancellationTokenSource();

        var options = new AzureBlobExportOptions();
        var destination = new ExportDataOptions<ExportDestinationType>(ExportDestinationType.AzureBlob, options);
        var operationId = Guid.NewGuid();
        IExportSink expected = Substitute.For<IExportSink>();

        IExportSinkProvider provider = Substitute.For<IExportSinkProvider>();
        provider.Type.Returns(ExportDestinationType.AzureBlob);
        provider.CreateAsync(options, operationId, tokenSource.Token).Returns(expected);

        var factory = new ExportSinkFactory(new IExportSinkProvider[] { provider });
        Assert.Same(expected, await factory.CreateAsync(destination, operationId, tokenSource.Token));

        await provider.Received(1).CreateAsync(options, operationId, tokenSource.Token);
    }

    [Fact]
    public async Task GivenNoProviders_WhenSecuring_ThenThrowException()
    {
        var factory = new ExportSinkFactory(Array.Empty<IExportSinkProvider>());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => factory.SecureSensitiveInfoAsync(
            new ExportDataOptions<ExportDestinationType>(ExportDestinationType.AzureBlob),
            Guid.NewGuid()));
    }

    [Fact]
    public async Task GivenValidProviders_WhenSecuring_ThenInvokeCorrectMethod()
    {
        using var tokenSource = new CancellationTokenSource();

        var operationId = Guid.NewGuid();
        var options = new AzureBlobExportOptions();
        var destination = new ExportDataOptions<ExportDestinationType>(ExportDestinationType.AzureBlob, options);
        var expected = new AzureBlobExportOptions();

        IExportSinkProvider provider = Substitute.For<IExportSinkProvider>();
        provider.Type.Returns(ExportDestinationType.AzureBlob);
        provider.SecureSensitiveInfoAsync(options, operationId, tokenSource.Token).Returns(expected);

        var factory = new ExportSinkFactory(new IExportSinkProvider[] { provider });
        ExportDataOptions<ExportDestinationType> actual = await factory.SecureSensitiveInfoAsync(destination, operationId, tokenSource.Token);

        await provider.Received(1).SecureSensitiveInfoAsync(options, operationId, tokenSource.Token);
        Assert.Equal(ExportDestinationType.AzureBlob, actual.Type);
        Assert.Same(expected, actual.Settings);
    }

    [Fact]
    public async Task GivenNoProviders_WhenValidating_ThenThrowException()
    {
        var factory = new ExportSinkFactory(Array.Empty<IExportSinkProvider>());
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => factory.ValidateAsync(new ExportDataOptions<ExportDestinationType>(ExportDestinationType.AzureBlob)));
    }

    [Fact]
    public async Task GivenValidProviders_WhenValidating_ThenInvokeCorrectMethod()
    {
        using var tokenSource = new CancellationTokenSource();

        var options = new AzureBlobExportOptions();
        var destination = new ExportDataOptions<ExportDestinationType>(ExportDestinationType.AzureBlob, options);

        IExportSinkProvider provider = Substitute.For<IExportSinkProvider>();
        provider.Type.Returns(ExportDestinationType.AzureBlob);

        var factory = new ExportSinkFactory(new IExportSinkProvider[] { provider });
        await factory.ValidateAsync(destination, tokenSource.Token);

        await provider.Received(1).ValidateAsync(options, tokenSource.Token);
    }
}
