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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Features.Export;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Export;
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

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task GivenCompletedOperation_WhenCleaningUp_SkipIfNoWork(bool configureStore, bool addSecret, bool expectDelete)
    {
        using var tokenSource = new CancellationTokenSource();

        var provider = configureStore
            ? new AzureBlobExportSinkProvider(_secretStore, Options.Create(_serializerOptions), NullLogger<AzureBlobExportSinkProvider>.Instance)
            : new AzureBlobExportSinkProvider(Options.Create(_serializerOptions), NullLogger<AzureBlobExportSinkProvider>.Instance);

        var options = new AzureBlobExportOptions
        {
            Secret = addSecret ? new SecretKey { Name = "secret" } : null
        };

        await provider.CompleteCopyAsync(options, tokenSource.Token);

        if (expectDelete)
            await _secretStore.Received(1).DeleteSecretAsync("secret", tokenSource.Token);
        else
            await _secretStore.DidNotReceiveWithAnyArgs().DeleteSecretAsync(default, default);
    }

    [Fact]
    public async Task GivenNoSecretStore_WhenCreatingSinkWithSecret_ThenThrow()
    {
        var containerUri = new Uri("https://unit-test.blob.core.windows.net/mycontainer?sv=2020-08-04&ss=b", UriKind.Absolute);
        var errorHref = new Uri($"https://unit-test.blob.core.windows.net/mycontainer/{Guid.NewGuid()}/Errors.log", UriKind.Absolute);
        var options = new AzureBlobExportOptions
        {
            Secret = new SecretKey { Name = "foo", Version = "bar" },
        };

        var provider = new AzureBlobExportSinkProvider(Options.Create(_serializerOptions), NullLogger<AzureBlobExportSinkProvider>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateAsync(_serviceProvider, options, Guid.NewGuid()));
        await _secretStore.DidNotReceiveWithAnyArgs().SetSecretAsync(default, default, default);
    }

    [Fact]
    public async Task GivenProvider_WhenCreatingSink_ThenCreateFromServiceContainer()
    {
        const string version = "1";
        var operationId = Guid.NewGuid();
        var containerUri = new Uri("https://unit-test.blob.core.windows.net/mycontainer?sv=2020-08-04&ss=b", UriKind.Absolute);
        var errorHref = new Uri($"https://unit-test.blob.core.windows.net/mycontainer/{operationId.ToString(OperationId.FormatSpecifier)}/Errors.log", UriKind.Absolute);
        var options = new AzureBlobExportOptions
        {
            Secret = new SecretKey
            {
                Name = operationId.ToString(OperationId.FormatSpecifier),
                Version = version,
            },
        };

        using var tokenSource = new CancellationTokenSource();

        // Note: Typically these values don't both exist together
        _secretStore
            .GetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), version, tokenSource.Token)
            .Returns(GetJson(containerUri));

        IExportSink sink = await _sinkProvider.CreateAsync(_serviceProvider, options, operationId, tokenSource.Token);

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
        var options = new AzureBlobExportOptions
        {
            ConnectionString = connectionString,
            BlobContainerUri = containerUri,
        };

        var provider = new AzureBlobExportSinkProvider(Options.Create(_serializerOptions), NullLogger<AzureBlobExportSinkProvider>.Instance);
        var actual = (AzureBlobExportOptions)await provider.SecureSensitiveInfoAsync(options, Guid.NewGuid());

        await _secretStore.DidNotReceiveWithAnyArgs().SetSecretAsync(default, default, default);

        Assert.Null(actual.Secret);
        Assert.Equal(connectionString, actual.ConnectionString);
        Assert.Equal(containerUri, actual.BlobContainerUri);
    }

    [Fact]
    public async Task GivenProvider_WhenSecuringInfo_ThenStoreSecrets()
    {
        const string version = "1";
        const string connectionString = "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar";
        var containerUri = new Uri("https://unit-test.blob.core.windows.net/mycontainer?sv=2020-08-04&ss=b", UriKind.Absolute);
        var operationId = Guid.NewGuid();

        // Note: Typically these values don't both exist together
        var options = new AzureBlobExportOptions
        {
            ConnectionString = connectionString,
            BlobContainerUri = containerUri,
        };

        using var tokenSource = new CancellationTokenSource();

        _secretStore
            .SetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), GetJson(connectionString, containerUri), tokenSource.Token)
            .Returns(version);

        var actual = (AzureBlobExportOptions)await _sinkProvider.SecureSensitiveInfoAsync(options, operationId, tokenSource.Token);

        await _secretStore
            .Received(1)
            .SetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), GetJson(connectionString, containerUri), tokenSource.Token);

        Assert.Equal(operationId.ToString(OperationId.FormatSpecifier), actual.Secret.Name);
        Assert.Equal(version, actual.Secret.Version);
    }

    private static string GetJson(Uri blobContainerUri)
        => $"{{\"blobContainerUri\":\"{JavaScriptEncoder.Default.Encode(blobContainerUri.AbsoluteUri)}\"}}";

    private static string GetJson(string connectionString, Uri blobContainerUri)
        => $"{{\"connectionString\":\"{JavaScriptEncoder.Default.Encode(connectionString)}\",\"blobContainerUri\":\"{JavaScriptEncoder.Default.Encode(blobContainerUri.AbsoluteUri)}\"}}";
}
