// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Export;

public class IdentifierExportSourceProviderTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IdentifierExportSourceProvider _provider;

    public IdentifierExportSourceProviderTests()
    {
        var services = new ServiceCollection();
        services.AddScoped(p => Substitute.For<IInstanceStore>());
        _serviceProvider = services.BuildServiceProvider();
        _provider = new IdentifierExportSourceProvider();
    }

    [Fact]
    public async Task GivenConfig_WhenCreatingSource_ThenReturnSource()
    {
        var options = new IdentifierExportOptions
        {
            Values = new string[]
            {
                "1/2/3",
                "45.6/789",
                "10.1112/13.14.15/16171819.20"
            },
        };

        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config.Set(options);

        IExportSource source = await _provider.CreateAsync(_serviceProvider, config, PartitionEntry.Default);
        Assert.IsType<IdentifierExportSource>(source);
    }

    [Fact]
    public async Task GivenInvalidConfig_WhenValidating_ThenThrow()
    {
        var options = new IdentifierExportOptions
        {
            Values = new string[]
            {
                "1/2/3",
                "45.6/789",
                "hello world",
            },
        };

        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config.Set(options);

        await Assert.ThrowsAsync<ValidationException>(() => _provider.ValidateAsync(config));
    }

    [Fact]
    public async Task GivenValidConfig_WhenValidating_ThenReturn()
    {
        var options = new IdentifierExportOptions
        {
            Values = new string[]
            {
                "1/2/3",
                "45.6/789",
                "10.1112/13.14.15/16171819.20"
            },
        };

        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config.Set(options);

        Assert.Same(config, await _provider.ValidateAsync(config));
    }
}
