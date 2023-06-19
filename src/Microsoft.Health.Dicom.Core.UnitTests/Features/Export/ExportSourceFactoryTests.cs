// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Models.Export;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Export;

public class ExportSourceFactoryTests
{
    [Fact]
    public async Task GivenNoProviders_WhenCreatingSource_ThenThrowException()
    {
        var factory = new ExportSourceFactory(Array.Empty<IExportSourceProvider>());
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => factory.CreateAsync(new ExportDataOptions<ExportSourceType>(ExportSourceType.Identifiers), Partition.Default));
    }

    [Fact]
    public async Task GivenValidProviders_WhenCreatingSource_ThenReturnSource()
    {
        using var tokenSource = new CancellationTokenSource();

        var options = new IdentifierExportOptions();
        var partition = Partition.Default;
        var source = new ExportDataOptions<ExportSourceType>(ExportSourceType.Identifiers, options);
        IExportSource expected = Substitute.For<IExportSource>();

        IExportSourceProvider provider = Substitute.For<IExportSourceProvider>();
        provider.Type.Returns(ExportSourceType.Identifiers);
        provider.CreateAsync(options, partition, tokenSource.Token).Returns(expected);

        var factory = new ExportSourceFactory(new IExportSourceProvider[] { provider });
        Assert.Same(expected, await factory.CreateAsync(source, partition, tokenSource.Token));

        await provider.Received(1).CreateAsync(options, partition, tokenSource.Token);
    }

    [Fact]
    public async Task GivenNoProviders_WhenValidating_ThenThrowException()
    {
        var factory = new ExportSourceFactory(Array.Empty<IExportSourceProvider>());
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => factory.ValidateAsync(new ExportDataOptions<ExportSourceType>(ExportSourceType.Identifiers)));
    }

    [Fact]
    public async Task GivenValidProviders_WhenValidating_ThenInvokeCorrectMethod()
    {
        using var tokenSource = new CancellationTokenSource();

        var options = new IdentifierExportOptions();
        var source = new ExportDataOptions<ExportSourceType>(ExportSourceType.Identifiers, options);

        IExportSourceProvider provider = Substitute.For<IExportSourceProvider>();
        provider.Type.Returns(ExportSourceType.Identifiers);

        var factory = new ExportSourceFactory(new IExportSourceProvider[] { provider });
        await factory.ValidateAsync(source, tokenSource.Token);

        await provider.Received(1).ValidateAsync(options, tokenSource.Token);
    }
}
