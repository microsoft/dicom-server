// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Core.Features.Identity;
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
    private readonly IFileStore _fileStore;
    private readonly IExternalCredentialProvider _credentialProvider;
    private readonly AzureBlobExportSinkProviderOptions _providerOptions;
    private readonly AzureBlobClientOptions _clientOptions;
    private readonly BlobOperationOptions _operationOptions;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly AzureBlobExportSinkProvider _sinkProvider;
    private readonly AzureBlobExportSinkProvider _secretlessSinkProvider;

    public AzureBlobExportSinkProviderTests()
    {
        _secretStore = Substitute.For<ISecretStore>();
        _fileStore = Substitute.For<IFileStore>();
        _credentialProvider = Substitute.For<IExternalCredentialProvider>();
        _providerOptions = new AzureBlobExportSinkProviderOptions { AllowPublicAccess = true, AllowSasTokens = true };
        _clientOptions = new AzureBlobClientOptions();
        _operationOptions = new BlobOperationOptions { Upload = new StorageTransferOptions() };
        _serializerOptions = new JsonSerializerOptions();
        _serializerOptions.ConfigureDefaultDicomSettings();

        _sinkProvider = new AzureBlobExportSinkProvider(
            _secretStore,
            _fileStore,
            _credentialProvider,
            CreateSnapshot(_providerOptions),
            CreateSnapshot(_clientOptions, AzureBlobExportSinkProvider.ClientOptionsName),
            CreateSnapshot(_operationOptions),
            CreateSnapshot(_serializerOptions),
            NullLogger<AzureBlobExportSinkProvider>.Instance);

        _secretlessSinkProvider = new AzureBlobExportSinkProvider(
            _fileStore,
            _credentialProvider,
            CreateSnapshot(_providerOptions),
            CreateSnapshot(_clientOptions, AzureBlobExportSinkProvider.ClientOptionsName),
            CreateSnapshot(_operationOptions),
            CreateSnapshot(_serializerOptions),
            NullLogger<AzureBlobExportSinkProvider>.Instance);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task GivenCompletedOperation_WhenCleaningUp_SkipIfNoWork(bool enableSecrets, bool addSecret, bool expectDelete)
    {
        using var tokenSource = new CancellationTokenSource();

        AzureBlobExportSinkProvider provider = enableSecrets ? _sinkProvider : _secretlessSinkProvider;
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
        var errorHref = new Uri($"https://unit-test.blob.core.windows.net/mycontainer/{Guid.NewGuid()}/errors.log", UriKind.Absolute);
        var options = new AzureBlobExportOptions
        {
            Secret = new SecretKey { Name = "foo", Version = "bar" },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _secretlessSinkProvider.CreateAsync(options, Guid.NewGuid()));
        await _secretStore.DidNotReceiveWithAnyArgs().GetSecretAsync(default, default, default);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GivenBlobContainerUri_WhenCreatingSink_ThenCreateFromServiceContainer(bool useManagedIdentity)
    {
        const string version = "1";
        var operationId = Guid.NewGuid();
        var containerUri = new Uri("https://unit-test.blob.core.windows.net/mycontainer?sv=2020-08-04&ss=b", UriKind.Absolute);
        var options = new AzureBlobExportOptions
        {
            Secret = new SecretKey
            {
                Name = operationId.ToString(OperationId.FormatSpecifier),
                Version = version,
            },
            UseManagedIdentity = useManagedIdentity,
        };

        using var tokenSource = new CancellationTokenSource();

        _secretStore
            .GetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), version, tokenSource.Token)
            .Returns(GetJson(containerUri));

        if (useManagedIdentity)
            _credentialProvider.GetTokenCredential().Returns(new DefaultAzureCredential());

        IExportSink sink = await _sinkProvider.CreateAsync(options, operationId, tokenSource.Token);

        Assert.IsType<AzureBlobExportSink>(sink);
        await _secretStore
            .Received(1)
            .GetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), version, tokenSource.Token);

        if (useManagedIdentity)
            _credentialProvider.Received(1).GetTokenCredential();
        else
            _credentialProvider.DidNotReceiveWithAnyArgs().GetTokenCredential();
    }

    [Fact]
    public async Task GivenConnectionString_WhenCreatingSink_ThenCreateFromServiceContainer()
    {
        const string version = "1";
        var operationId = Guid.NewGuid();
        var connectionString = "BlobEndpoint=https://unit-test.blob.core.windows.net/;SharedAccessSignature=sastoken";
        var options = new AzureBlobExportOptions
        {
            BlobContainerName = "mycontainer",
            Secret = new SecretKey
            {
                Name = operationId.ToString(OperationId.FormatSpecifier),
                Version = version,
            },
        };

        using var tokenSource = new CancellationTokenSource();

        _secretStore
            .GetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), version, tokenSource.Token)
            .Returns(GetJson(connectionString));

        IExportSink sink = await _sinkProvider.CreateAsync(options, operationId, tokenSource.Token);

        Assert.IsType<AzureBlobExportSink>(sink);
        await _secretStore
            .Received(1)
            .GetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), version, tokenSource.Token);
    }

    [Theory]
    [InlineData("BlobEndpoint=https://unit-test.blob.core.windows.net/;SharedAccessSignature=sastoken", "export-e2e-test", null)]
    [InlineData(null, null, "https://dcmcipermanpmlxszw4sayty.blob.core.windows.net/export-e2e-test?sig=sastoken")]
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Cannot use inline Uri")]
    public async Task GivenSasTokenWithNoSecretStore_WhenSecuringInfo_ThenThrow(string connectionString, string blobContainerName, string blobContainerUri)
    {
        var operationId = Guid.NewGuid();
        var options = new AzureBlobExportOptions
        {
            BlobContainerName = blobContainerName,
            BlobContainerUri = blobContainerUri != null ? new Uri(blobContainerUri, UriKind.Absolute) : null,
            ConnectionString = connectionString,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _secretlessSinkProvider.SecureSensitiveInfoAsync(options, operationId));
        await _secretStore.DidNotReceiveWithAnyArgs().SetSecretAsync(default, default, default, default);
    }

    [Theory]
    [InlineData("BlobEndpoint=https://unit-test.blob.core.windows.net/;", "export-e2e-test", null)]
    [InlineData(null, null, "https://dcmcipermanpmlxszw4sayty.blob.core.windows.net/export-e2e-test")]
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Cannot use inline Uri")]
    public async Task GivenNoSasToken_WhenSecuringInfo_ThenSkip(string connectionString, string blobContainerName, string blobContainerUri)
    {
        var options = new AzureBlobExportOptions
        {
            BlobContainerName = blobContainerName,
            BlobContainerUri = blobContainerUri != null ? new Uri(blobContainerUri, UriKind.Absolute) : null,
            ConnectionString = connectionString,
        };

        var actual = (AzureBlobExportOptions)await _sinkProvider.SecureSensitiveInfoAsync(options, Guid.NewGuid());

        await _secretStore.DidNotReceiveWithAnyArgs().SetSecretAsync(default, default, default, default);

        Assert.Null(actual.Secret);
        Assert.Equal(blobContainerName, actual.BlobContainerName);
        Assert.Equal(blobContainerUri, actual.BlobContainerUri?.AbsoluteUri);
        Assert.Equal(connectionString, actual.ConnectionString);
    }

    [Theory]
    [InlineData("BlobEndpoint=https://unit-test.blob.core.windows.net/;SharedAccessSignature=sastoken", "export-e2e-test", null)]
    [InlineData(null, null, "https://dcmcipermanpmlxszw4sayty.blob.core.windows.net/export-e2e-test?sig=sastoken")]
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Cannot use inline Uri")]
    public async Task GivenSasToken_WhenSecuringInfo_ThenStoreSecrets(string connectionString, string blobContainerName, string blobContainerUri)
    {
        const string version = "1";
        var operationId = Guid.NewGuid();
        var options = new AzureBlobExportOptions
        {
            BlobContainerName = blobContainerName,
            BlobContainerUri = blobContainerUri != null ? new Uri(blobContainerUri, UriKind.Absolute) : null,
            ConnectionString = connectionString,
        };
        string secretJson = blobContainerUri != null ? GetJson(options.BlobContainerUri) : GetJson(connectionString);

        using var tokenSource = new CancellationTokenSource();

        _secretStore
            .SetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), secretJson, MediaTypeNames.Application.Json, tokenSource.Token)
            .Returns(version);

        var actual = (AzureBlobExportOptions)await _sinkProvider.SecureSensitiveInfoAsync(options, operationId, tokenSource.Token);

        await _secretStore
            .Received(1)
            .SetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), secretJson, MediaTypeNames.Application.Json, tokenSource.Token);

        Assert.Equal(operationId.ToString(OperationId.FormatSpecifier), actual.Secret.Name);
        Assert.Equal(version, actual.Secret.Version);
        Assert.Null(options.BlobContainerUri);
        Assert.Null(options.ConnectionString);
    }

    [Theory]
    [InlineData(null, "  ", "mycontainer", false)]
    [InlineData(null, "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar", null, false)]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer", "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar", null, false)]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer", null, "mycontainer", false)]
    [InlineData(null, "BlobEndpoint=https://unit-test.blob.core.windows.net/;AccountKey=abc123", "mycontainer", false)]
    [InlineData(null, "BlobEndpoint=https://unit-test.blob.core.windows.net/;SharedAccessSignature=abc123", "mycontainer", true)]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer?sig=foo", null, null, true)]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer?foo=bar&sig=baz", null, null, true)]
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "URIs cannot be used inline.")]
    public async Task GivenInvalidOptions_WhenValidating_ThrowValidationException(
        string blobContainerUri,
        string connectionString,
        string blobContainerName,
        bool useManagedIdentity)
    {
        var options = new AzureBlobExportOptions
        {
            ConnectionString = connectionString,
            BlobContainerName = blobContainerName,
            BlobContainerUri = blobContainerUri != null ? new Uri(blobContainerUri, UriKind.Absolute) : null,
            UseManagedIdentity = useManagedIdentity,
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sinkProvider.ValidateAsync(options));
    }

    [Theory]
    [InlineData("BlobEndpoint=https://unit-test.blob.core.windows.net/", "export-e2e-test", null)]
    [InlineData(null, null, "https://dcmcipermanpmlxszw4sayty.blob.core.windows.net/export-e2e-test")]
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Cannot use inline Uri")]
    public async Task GivenPublicAccess_WhenDisallowed_ThrowValidationException(string connectionString, string blobContainerName, string blobContainerUri)
    {
        var options = new AzureBlobExportOptions
        {
            BlobContainerName = blobContainerName,
            BlobContainerUri = blobContainerUri != null ? new Uri(blobContainerUri, UriKind.Absolute) : null,
            ConnectionString = connectionString,
            UseManagedIdentity = false, // Be explicit
        };

        _providerOptions.AllowPublicAccess = false;
        await Assert.ThrowsAsync<ValidationException>(() => _sinkProvider.ValidateAsync(options));
    }

    [Theory]
    [InlineData("BlobEndpoint=https://unit-test.blob.core.windows.net/;SharedAccessSignature=sastoken", "export-e2e-test", null)]
    [InlineData(null, null, "https://dcmcipermanpmlxszw4sayty.blob.core.windows.net/export-e2e-test?sig=sastoken")]
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Cannot use inline Uri")]
    public async Task GivenSasToken_WhenDisallowed_ThrowValidationException(string connectionString, string blobContainerName, string blobContainerUri)
    {
        var options = new AzureBlobExportOptions
        {
            BlobContainerName = blobContainerName,
            BlobContainerUri = blobContainerUri != null ? new Uri(blobContainerUri, UriKind.Absolute) : null,
            ConnectionString = connectionString,
        };

        _providerOptions.AllowSasTokens = false;
        await Assert.ThrowsAsync<ValidationException>(() => _sinkProvider.ValidateAsync(options));
    }

    private static IOptionsSnapshot<T> CreateSnapshot<T>(T options, string name = "") where T : class
    {
        IOptionsSnapshot<T> snapshot = Substitute.For<IOptionsSnapshot<T>>();
        snapshot.Get(name).Returns(options);
        if (name == "")
            snapshot.Value.Returns(options);

        return snapshot;
    }

    private static string GetJson(Uri blobContainerUri)
        => $"{{\"blobContainerUri\":\"{JavaScriptEncoder.Default.Encode(blobContainerUri.AbsoluteUri)}\"}}";

    private static string GetJson(string connectionString)
        => $"{{\"connectionString\":\"{JavaScriptEncoder.Default.Encode(connectionString)}\"}}";
}
