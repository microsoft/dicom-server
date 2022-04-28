// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
    public void GivenNoProviders_WhenCreatingSource_ThenThrowException()
    {
        var factory = new ExportSourceFactory(Substitute.For<IServiceProvider>(), Array.Empty<IExportSourceProvider>());
        Assert.Throws<KeyNotFoundException>(() => factory.CreateSource(new TypedConfiguration<ExportSourceType> { Type = ExportSourceType.Identifiers }, PartitionEntry.Default));
    }

    [Fact]
    public void GivenValidProviders_WhenCreatingSource_ThenReturnSource()
    {
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        IConfiguration config = Substitute.For<IConfiguration>();
        var partition = PartitionEntry.Default;
        var source = new TypedConfiguration<ExportSourceType> { Type = ExportSourceType.Identifiers, Configuration = config };
        IExportSource expected = Substitute.For<IExportSource>();

        IExportSourceProvider provider = Substitute.For<IExportSourceProvider>();
        provider.Type.Returns(ExportSourceType.Identifiers);
        provider.Create(serviceProvider, config, partition).Returns(expected);

        var factory = new ExportSourceFactory(serviceProvider, new IExportSourceProvider[] { provider });
        Assert.Same(expected, factory.CreateSource(source, partition));

        provider.Received(1).Create(serviceProvider, config, partition);
    }

    [Fact]
    public void GivenNoProviders_WhenValidating_ThenThrowException()
    {
        var factory = new ExportSourceFactory(Substitute.For<IServiceProvider>(), Array.Empty<IExportSourceProvider>());
        Assert.Throws<KeyNotFoundException>(() => factory.Validate(new TypedConfiguration<ExportSourceType> { Type = ExportSourceType.Identifiers }));
    }

    [Fact]
    public void GivenValidProviders_WhenValidate_ThenInvokeCorrectMethod()
    {
        IConfiguration config = Substitute.For<IConfiguration>();
        var source = new TypedConfiguration<ExportSourceType> { Type = ExportSourceType.Identifiers, Configuration = config };

        IExportSourceProvider provider = Substitute.For<IExportSourceProvider>();
        provider.Type.Returns(ExportSourceType.Identifiers);

        var factory = new ExportSourceFactory(Substitute.For<IServiceProvider>(), new IExportSourceProvider[] { provider });
        factory.Validate(source);

        provider.Received(1).Validate(config);
    }
}
