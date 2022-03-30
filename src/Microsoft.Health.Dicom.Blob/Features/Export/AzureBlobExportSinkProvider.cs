// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

public class AzureBlobExportSinkProvider : IExportSinkProvider
{
    public ExportDestinationType Type => ExportDestinationType.AzureBlob;

    public IExportSink Create(IServiceProvider provider, IConfiguration config)
    {
        // source objects
        var sourceBlobServiceClient = provider.GetService<BlobServiceClient>();
        var blobOptions = provider.GetService<IOptions<BlobOperationOptions>>();
        var blobContainerConfig = provider.GetService<IOptionsMonitor<BlobContainerConfiguration>>();

        // destination objects
        InitializeDestinationStore(config, out BlobContainerClient destBlobContainerClient, out string destPath);

        // init and return
        BlobCopyStore store = new BlobCopyStore(sourceBlobServiceClient, blobContainerConfig, blobOptions, destBlobContainerClient, destPath);
        return new AzureBlobExportSink(store);
    }

    public void Validate(IConfiguration config)
    {
        AzureBlobExportOptions options = config.Get<AzureBlobExportOptions>();

        if (options.ContainerUri != null && options.ContainerSasUri != null)
            throw new FormatException();
        else if (options.ContainerUri == null && options.ContainerSasUri == null)
            throw new FormatException();
    }

    private static void InitializeDestinationStore(IConfiguration config, out BlobContainerClient blobContainerClient, out string path)
    {
        var blobClientOptions = config.Get<BlobServiceClientOptions>();
        var exportConfig = config.Get<AzureBlobExportOptions>();

        path = exportConfig.FolderPath;

        if (exportConfig.ContainerUri != null)
        {
            throw new NotImplementedException();
            //need a way to pass the MI config from KeyVault to here
            //DefaultAzureCredential credential = new DefaultAzureCredential(blobClientOptions.Credentials);
            //blobContainerClient = new BlobContainerClient(exportConfig.ContainerUri, credential, blobClientOptions);
        }
        else
        {
            blobContainerClient = new BlobContainerClient(exportConfig.ContainerSasUri, blobClientOptions);
        }
    }
}
