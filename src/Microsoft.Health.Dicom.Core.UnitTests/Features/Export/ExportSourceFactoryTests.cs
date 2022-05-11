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
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Export;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Export;

public class ExportSourceFactoryTests
{
    [Fact]
    public async Task GivenNoProviders_WhenCreatingSource_ThenThrowException()
    {
        var factory = new ExportSourceFactory(Substitute.For<IServiceProvider>(), Array.Empty<IExportSourceProvider>());
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => factory.CreateAsync(new TypedConfiguration<ExportSourceType> { Type = ExportSourceType.Identifiers }, PartitionEntry.Default));
    }

    [Fact]
    public async Task GivenValidProviders_WhenCreatingSource_ThenReturnSource()
    {
        using var tokenSource = new CancellationTokenSource();

        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        IConfiguration config = Substitute.For<IConfiguration>();
        var partition = PartitionEntry.Default;
        var source = new TypedConfiguration<ExportSourceType> { Type = ExportSourceType.Identifiers, Configuration = config };
        IExportSource expected = Substitute.For<IExportSource>();

        IExportSourceProvider provider = Substitute.For<IExportSourceProvider>();
        provider.Type.Returns(ExportSourceType.Identifiers);
        provider.CreateAsync(serviceProvider, config, partition, tokenSource.Token).Returns(expected);

        var factory = new ExportSourceFactory(serviceProvider, new IExportSourceProvider[] { provider });
        Assert.Same(expected, await factory.CreateAsync(source, partition, tokenSource.Token));

        await provider.Received(1).CreateAsync(serviceProvider, config, partition, tokenSource.Token);
    }

    [Fact]
    public async Task GivenNoProviders_WhenValidating_ThenThrowException()
    {
        var factory = new ExportSourceFactory(Substitute.For<IServiceProvider>(), Array.Empty<IExportSourceProvider>());
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => factory.ValidateAsync(new TypedConfiguration<ExportSourceType> { Type = ExportSourceType.Identifiers }));
    }

    [Fact]
    public async Task GivenValidProviders_WhenValidate_ThenInvokeCorrectMethod()
    {
        using var tokenSource = new CancellationTokenSource();

        IConfiguration config = Substitute.For<IConfiguration>();
        var source = new TypedConfiguration<ExportSourceType> { Type = ExportSourceType.Identifiers, Configuration = config };
        IConfiguration expectedConfig = Substitute.For<IConfiguration>();

        IExportSourceProvider provider = Substitute.For<IExportSourceProvider>();
        provider.Type.Returns(ExportSourceType.Identifiers);
        provider.ValidateAsync(config, tokenSource.Token).Returns(expectedConfig);

        var factory = new ExportSourceFactory(Substitute.For<IServiceProvider>(), new IExportSourceProvider[] { provider });
        TypedConfiguration<ExportSourceType> actual = await factory.ValidateAsync(source, tokenSource.Token);

        await provider.Received(1).ValidateAsync(config, tokenSource.Token);
        Assert.Equal(ExportSourceType.Identifiers, actual.Type);
        Assert.Same(expectedConfig, actual.Configuration);
    }
}
