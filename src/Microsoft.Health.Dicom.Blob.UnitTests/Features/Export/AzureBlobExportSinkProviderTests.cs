// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly AzureBlobExportSinkProvider _sinkProvider;
    private readonly IServiceProvider _serviceProvider;

    public AzureBlobExportSinkProviderTests()
    {
        _secretStore = Substitute.For<ISecretStore>();
        _sinkProvider = new AzureBlobExportSinkProvider(_secretStore);

        var services = new ServiceCollection();
        services.AddScoped(p => Substitute.For<IFileStore>());
        services.AddOptions<AzureBlobClientOptions>("Export");
        services.Configure<BlobOperationOptions>(o => o.Upload = new StorageTransferOptions());

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task GivenProvider_WhenCreatingSink_ThenCreateFromServiceContainer()
    {
        var operationId = Guid.NewGuid();
        var containerUri = new Uri("https://unit-test.blob.core.windows.net/mycontainer", UriKind.Absolute);
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

        _secretStore
            .GetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), version, tokenSource.Token)
            .Returns(GetJson(containerUri));

        IExportSink sink = await _sinkProvider.CreateSinkAsync(_serviceProvider, configuration, operationId, tokenSource.Token);

        Assert.IsType<AzureBlobExportSink>(sink);
        await _secretStore
            .Received(1)
            .GetSecretAsync(operationId.ToString(OperationId.FormatSpecifier), version, tokenSource.Token);
    }

    private static string GetJson(Uri containerUri)
        => $"{{\"ContainerUri\":\"{JavaScriptEncoder.Default.Encode(containerUri.AbsoluteUri)}\"}}";
}
