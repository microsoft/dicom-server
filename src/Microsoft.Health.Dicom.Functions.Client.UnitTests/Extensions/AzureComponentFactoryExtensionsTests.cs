// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Functions.Client.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.Extensions;

public class AzureComponentFactoryExtensionsTests
{
    private const string AccountName = "unittest";
    private const string SectionName = "AzureWebJobsStorage";

    private readonly AzureComponentFactory _factory;

    public AzureComponentFactoryExtensionsTests()
    {
        ServiceCollection services = new ServiceCollection();

        services
            .AddLogging()
            .AddAzureClientsCore();

        _factory = services.BuildServiceProvider().GetRequiredService<AzureComponentFactory>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenBasicConfiguration_WhenCreatingBlobServiceClient_ThenCreateClient(bool direct)
    {
        IConfiguration config = CreateConnectionStringConfiguration(SectionName, direct);
        BlobServiceClient actual = _factory.CreateBlobServiceClient(config.GetSection(SectionName));
        Assert.Equal(new Uri("http://127.0.0.1:10000/devstoreaccount1"), actual.Uri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenBasicConfiguration_WhenCreatingQueueServiceClient_ThenCreateClient(bool direct)
    {
        IConfiguration config = CreateConnectionStringConfiguration(SectionName, direct);
        QueueServiceClient actual = _factory.CreateQueueServiceClient(config.GetSection(SectionName));
        Assert.Equal(new Uri("http://127.0.0.1:10001/devstoreaccount1"), actual.Uri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenBasicConfiguration_WhenCreatingTableServiceClient_ThenCreateClient(bool direct)
    {
        IConfiguration config = CreateConnectionStringConfiguration(SectionName, direct);
        TableServiceClient actual = _factory.CreateTableServiceClient(config.GetSection(SectionName));
        Assert.Equal(new Uri("http://127.0.0.1:10002/devstoreaccount1"), actual.Uri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenManagedIdentityConfiguration_WhenCreatingBlobServiceClient_ThenCreateClient(bool specifyServiceUri)
    {
        IConfiguration config = CreateManagedIdentityConfiguration(SectionName, AccountName, specifyServiceUri ? "blob" : null);
        BlobServiceClient actual = _factory.CreateBlobServiceClient(config.GetSection(SectionName));
        Assert.Equal(new Uri($"https://{AccountName}.blob.core.windows.net"), actual.Uri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenManagedIdentityConfiguration_WhenCreatingQueueServiceClient_ThenCreateClient(bool specifyServiceUri)
    {
        IConfiguration config = CreateManagedIdentityConfiguration(SectionName, AccountName, specifyServiceUri ? "queue" : null);
        QueueServiceClient actual = _factory.CreateQueueServiceClient(config.GetSection(SectionName));
        Assert.Equal(new Uri($"https://{AccountName}.queue.core.windows.net"), actual.Uri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenManagedIdentityConfiguration_WhenCreatingTableServiceClient_ThenCreateClient(bool specifyServiceUri)
    {
        IConfiguration config = CreateManagedIdentityConfiguration(SectionName, AccountName, specifyServiceUri ? "table" : null);
        TableServiceClient actual = _factory.CreateTableServiceClient(config.GetSection(SectionName));
        Assert.Equal(new Uri($"https://{AccountName}.table.core.windows.net"), actual.Uri);
    }

    private static IConfiguration CreateConnectionStringConfiguration(string section, bool direct)
    {
        string key = direct ? section : section + ":ConnectionString";
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new KeyValuePair<string, string>[]
                {
                    KeyValuePair.Create(key, "UseDevelopmentStorage=true"),
                })
            .Build();
    }

    private static IConfiguration CreateManagedIdentityConfiguration(string section, string accountName, string service = null)
    {
        string prefix = section + ":";
        KeyValuePair<string, string> connectionProperty = service is null
            ? KeyValuePair.Create(prefix + "AccountName", accountName)
            : KeyValuePair.Create(prefix + service + "ServiceUri", $"https://{accountName}.{service}.core.windows.net");

        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new KeyValuePair<string, string>[]
                {
                    connectionProperty,
                    KeyValuePair.Create(prefix + "ClientId", Guid.NewGuid().ToString()),
                    KeyValuePair.Create(prefix + "Credential", "ManagedIdentity"),
                })
            .Build();
    }
}
