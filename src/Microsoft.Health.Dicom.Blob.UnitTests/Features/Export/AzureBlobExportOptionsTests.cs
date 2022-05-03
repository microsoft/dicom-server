// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Blob.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Models;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.Export;

public class AzureBlobExportOptionsTests
{
    [Theory]
    [InlineData(null, "  ", "mycontainer", "%SopInstance%.dcm", "%Operation%/Errors.log")]
    [InlineData(null, "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar", null, "%SopInstance%.dcm", "%Operation%/Errors.log")]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer", "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar", null, "%SopInstance%.dcm", "%Operation%/Errors.log")]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer", null, "mycontainer", "%SopInstance%.dcm", "%Operation%/Errors.log")]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer", null, null, null, "%Operation%/Errors.log")]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer", null, null, "%SopInstance%.dcm", "")]
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "URIs cannot be used inline.")]
    public void GivenInvalidOptions_WhenValidating_ThenReturnFailures(
        string containerUri,
        string connectionString,
        string containerName,
        string filePattern,
        string errorPattern)
    {
        var options = new AzureBlobExportOptions
        {
            ConnectionString = connectionString,
            ContainerName = containerName,
            ContainerUri = containerUri != null ? new Uri(containerUri, UriKind.Absolute) : null,
            DicomFilePattern = filePattern,
            ErrorLogPattern = errorPattern,
        };

        Assert.Single(options.Validate(null).ToList());
    }

    [Fact]
    public async Task GivenSensitiveInfo_WhenClassifying_ThenStoreSecrets()
    {
        const string secretName = "MySecret";
        const string version = "123";
        using var tokenSource = new CancellationTokenSource();

        // Note: Typically these values don't both exist together
        var options = new AzureBlobExportOptions
        {
            ConnectionString = "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar",
            ContainerUri = new Uri("https://unit-test.blob.core.windows.net/mycontainer?sv=2020-08-04&ss=b", UriKind.Absolute),
        };

        ISecretStore store = Substitute.For<ISecretStore>();
        string json = GetJson(options.ConnectionString, options.ContainerUri);
        store.SetSecretAsync(secretName, json, tokenSource.Token).Returns(version);

        await options.ClassifyAsync(store, secretName, tokenSource.Token);

        await store.Received(1).SetSecretAsync(secretName, json, tokenSource.Token);

        Assert.Equal(secretName, options.Secrets.Name);
        Assert.Equal(version, options.Secrets.Version);
        Assert.Null(options.ConnectionString);
        Assert.Null(options.ContainerUri);
    }

    [Fact]
    public async Task GivenNoSecret_WhenDeclassifying_ThenSkip()
    {
        using var tokenSource = new CancellationTokenSource();
        ISecretStore store = Substitute.For<ISecretStore>();

        await new AzureBlobExportOptions().DeclassifyAsync(store, tokenSource.Token);

        await store.DidNotReceiveWithAnyArgs().GetSecretAsync(default, default, default);
    }

    [Fact]
    public async Task GivenSecret_WhenDeclassifying_ThenRetrieveValues()
    {
        const string secretName = "MySecret";
        const string version = "123";
        const string connectionString = "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar";
        var containerUri = new Uri("https://unit-test.blob.core.windows.net/mycontainer?sv=2020-08-04&ss=b", UriKind.Absolute);
        using var tokenSource = new CancellationTokenSource();

        // Note: Typically these values don't both exist together
        var options = new AzureBlobExportOptions
        {
            Secrets = new SecretKey
            {
                Name = secretName,
                Version = version,
            },
        };

        ISecretStore store = Substitute.For<ISecretStore>();
        string json = GetJson(connectionString, containerUri);
        store.GetSecretAsync(secretName, version, tokenSource.Token).Returns(json);

        await options.DeclassifyAsync(store, tokenSource.Token);

        await store.Received(1).GetSecretAsync(secretName, version, tokenSource.Token);

        Assert.Equal(connectionString, options.ConnectionString);
        Assert.Equal(containerUri, options.ContainerUri);
        Assert.Null(options.Secrets);
    }

    private static string GetJson(string connectionString, Uri containerUri)
        => $"{{\"ConnectionString\":\"{JavaScriptEncoder.Default.Encode(connectionString)}\",\"ContainerUri\":\"{JavaScriptEncoder.Default.Encode(containerUri.AbsoluteUri)}\"}}";
}
