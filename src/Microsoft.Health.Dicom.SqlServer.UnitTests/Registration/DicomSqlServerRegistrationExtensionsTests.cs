// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.SqlServer.Registration;
using Microsoft.Health.SqlServer.Configs;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Registration;

[Collection("SQL Authentication Collection")]
public sealed class DicomSqlServerRegistrationExtensionsTests : IDisposable
{
    [Fact]
    public void GivenWebServerBuilder_WhenUsingDefaults_ThenUseBuiltInAuthenticationProvider()
    {
        IServiceCollection services = new ServiceCollection();
        IDicomServerBuilder builder = Substitute.For<IDicomServerBuilder>();
        builder.Services.Returns(services);

        IConfiguration configuration = new ConfigurationBuilder().Build();

        builder.AddSqlServer(configuration);

        Assert.IsType<ActiveDirectoryAuthenticationProvider>(SqlAuthenticationProvider.GetProvider(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity));
        Assert.IsType<ActiveDirectoryAuthenticationProvider>(SqlAuthenticationProvider.GetProvider(SqlAuthenticationMethod.ActiveDirectoryMSI));
    }

    [Fact]
    public void GivenFunctionsBuilder_WhenUsingDefaults_ThenUseBuiltInAuthenticationProvider()
    {
        IServiceCollection services = new ServiceCollection();
        IDicomFunctionsBuilder builder = Substitute.For<IDicomFunctionsBuilder>();
        builder.Services.Returns(services);

        IConfiguration configuration = new ConfigurationBuilder()
            .Build();

        builder.AddSqlServer(configuration.Bind);

        Assert.IsType<ActiveDirectoryAuthenticationProvider>(SqlAuthenticationProvider.GetProvider(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity));
        Assert.IsType<ActiveDirectoryAuthenticationProvider>(SqlAuthenticationProvider.GetProvider(SqlAuthenticationMethod.ActiveDirectoryMSI));
    }

    [Fact]
    public void GivenWebServerBuilder_WhenConfiguringWorkloadIdenity_ThenIncludeAuthenticationProvider()
    {
        IServiceCollection services = new ServiceCollection();
        IDicomServerBuilder builder = Substitute.For<IDicomServerBuilder>();
        builder.Services.Returns(services);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>($"{SqlServerDataStoreConfiguration.SectionName}:EnableWorkloadIdentity", "true"),
            })
            .Build();

        builder.AddSqlServer(configuration);

        Assert.IsNotType<ActiveDirectoryAuthenticationProvider>(SqlAuthenticationProvider.GetProvider(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity));
        Assert.IsNotType<ActiveDirectoryAuthenticationProvider>(SqlAuthenticationProvider.GetProvider(SqlAuthenticationMethod.ActiveDirectoryMSI));
    }

    [Fact]
    public void GivenFunctionBuilder_WhenConfiguringWorkloadIdenity_ThenIncludeAuthenticationProvider()
    {
        IServiceCollection services = new ServiceCollection();
        IDicomFunctionsBuilder builder = Substitute.For<IDicomFunctionsBuilder>();
        builder.Services.Returns(services);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>("EnableWorkloadIdentity", "true"),
            })
            .Build();

        builder.AddSqlServer(configuration.Bind);

        Assert.IsNotType<ActiveDirectoryAuthenticationProvider>(SqlAuthenticationProvider.GetProvider(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity));
        Assert.IsNotType<ActiveDirectoryAuthenticationProvider>(SqlAuthenticationProvider.GetProvider(SqlAuthenticationMethod.ActiveDirectoryMSI));
    }

    public void Dispose()
    {
        ActiveDirectoryAuthenticationProvider provider = new();
        SqlAuthenticationProvider.SetProvider(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity, provider);
        SqlAuthenticationProvider.SetProvider(SqlAuthenticationMethod.ActiveDirectoryMSI, provider);
    }
}
