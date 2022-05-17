// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Features.Export;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.Export;

public class AzureBlobExportSinkProviderTests
{
    private readonly ISecretStore _secretStore;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly AzureBlobExportSinkProvider _sinkProvider;
    private readonly IServiceProvider _serviceProvider;

    public AzureBlobExportSinkProviderTests()
    {
        _secretStore = Substitute.For<ISecretStore>();
        _serializerOptions = new JsonSerializerOptions();
        _serializerOptions.ConfigureDefaultDicomSettings();
        _sinkProvider = new AzureBlobExportSinkProvider(_secretStore, Options.Create(_serializerOptions), NullLogger<AzureBlobExportSinkProvider>.Instance);

        var services = new ServiceCollection();
        services.AddScoped(p => Substitute.For<IFileStore>());
        services.AddOptions<AzureBlobClientOptions>("Export");
        services.Configure<BlobOperationOptions>(o => o.Upload = new StorageTransferOptions());
        services.Configure<JsonSerializerOptions>(o => o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task GivenNoSecretStore_WhenCreatingSinkWithSecret_ThenThrow()
    {
        var containerUri = new Uri("https://unit-test.blob.core.windows.net/mycontainer?sv=2020-08-04&ss=b", UriKind.Absolute);
        var errorHref = new Uri($"https://unit-test.blob.core.windows.net/mycontainer/{Guid.NewGuid()}/Errors.log", UriKind.Absolute);

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        configuration.Set(
            new AzureBlobExportOptions
            {
                Secrets = new SecretKey { Name = "foo", Version = "bar" },
            },
            c => c.BindNonPublicProperties = true);

        var provider = new AzureBlobExportSinkProvider(Options.Create(_serializerOptions), NullLogger<AzureBlobExportSinkProvider>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateAsync(_serviceProvider, configuration, Guid.NewGuid()));
        await _secretStore.DidNotReceiveWithAnyArgs().SetSecretAsync(default, default, default);
    }

    [Fact]
    public async Task GivenProvider_WhenCreatingSink_ThenCreateFromServiceContainer()
    {
        var operationId = Guid.NewGuid();
        var containerUri = new Uri("https://unit-test.blob.core.windows.net/mycontainer?sv=2020-08-04&ss=b", UriKind.Absolute);
        var errorHref = new Uri($"https://unit-test.blob.core.windows.net/mycontainer/{operationId.ToString(OperationId.FormatSpecifier)}/Errors.log", UriKind.Absolute);
        const string version = "1";

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        configuration.Set(
            new AzureBlobExportOptions
            {
                Secrets = new SecretKey
                {
                    Name = operationId.ToString(OperationId.FormatSpecifier),
                    Version = version,
                },
            },
            c => c.BindNonPublicProperties = true);

        using var tokenSource = new CancellationTokenSource();

        // Note: Typically these values don't both exist together
        _secretStore
            .GetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), version, tokenSource.Token)
            .Returns(GetJson(containerUri));

        IExportSink sink = await _sinkProvider.CreateAsync(_serviceProvider, configuration, operationId, tokenSource.Token);

        Assert.IsType<AzureBlobExportSink>(sink);
        Assert.Equal(errorHref, sink.ErrorHref);
        await _secretStore
            .Received(1)
            .GetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), version, tokenSource.Token);
    }

    [Fact]
    public async Task GivenNoSecretStore_WhenSecuringInfo_ThenSkip()
    {
        const string connectionString = "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar";
        var containerUri = new Uri("https://unit-test.blob.core.windows.net/mycontainer?sv=2020-08-04&ss=b", UriKind.Absolute);

        // Note: Typically these values don't both exist together
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        configuration[nameof(AzureBlobExportOptions.ContainerUri)] = containerUri.AbsoluteUri;
        configuration[nameof(AzureBlobExportOptions.ConnectionString)] = connectionString;

        var provider = new AzureBlobExportSinkProvider(Options.Create(_serializerOptions), NullLogger<AzureBlobExportSinkProvider>.Instance);
        IConfiguration actualConfig = await provider.SecureSensitiveInfoAsync(configuration, Guid.NewGuid());

        await _secretStore.DidNotReceiveWithAnyArgs().SetSecretAsync(default, default, default);

        AzureBlobExportOptions actual = actualConfig.Get<AzureBlobExportOptions>(c => c.BindNonPublicProperties = true);
        Assert.Null(actual.Secrets);
        Assert.Equal(connectionString, actual.ConnectionString);
        Assert.Equal(containerUri, actual.ContainerUri);
    }

    [Fact]
    public async Task GivenProvider_WhenSecuringInfo_ThenStoreSecrets()
    {
        const string version = "1";
        const string connectionString = "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar";
        var containerUri = new Uri("https://unit-test.blob.core.windows.net/mycontainer?sv=2020-08-04&ss=b", UriKind.Absolute);
        var operationId = Guid.NewGuid();

        // Note: Typically these values don't both exist together
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        configuration[nameof(AzureBlobExportOptions.ContainerUri)] = containerUri.AbsoluteUri;
        configuration[nameof(AzureBlobExportOptions.ConnectionString)] = connectionString;

        using var tokenSource = new CancellationTokenSource();

        _secretStore
            .SetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), GetJson(connectionString, containerUri), tokenSource.Token)
            .Returns(version);

        IConfiguration actualConfig = await _sinkProvider.SecureSensitiveInfoAsync(configuration, operationId, tokenSource.Token);

        await _secretStore
            .Received(1)
            .SetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), GetJson(connectionString, containerUri), tokenSource.Token);

        AzureBlobExportOptions actual = actualConfig.Get<AzureBlobExportOptions>(c => c.BindNonPublicProperties = true);
        Assert.Equal(operationId.ToString(OperationId.FormatSpecifier), actual.Secrets.Name);
        Assert.Equal(version, actual.Secrets.Version);
    }

    private static string GetJson(Uri containerUri)
        => $"{{\"containerUri\":\"{JavaScriptEncoder.Default.Encode(containerUri.AbsoluteUri)}\"}}";

    private static string GetJson(string connectionString, Uri containerUri)
        => $"{{\"connectionString\":\"{JavaScriptEncoder.Default.Encode(connectionString)}\",\"containerUri\":\"{JavaScriptEncoder.Default.Encode(containerUri.AbsoluteUri)}\"}}";
}
