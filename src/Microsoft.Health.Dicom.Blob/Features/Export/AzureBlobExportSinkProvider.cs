// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

public class AzureBlobExportSinkProvider : IExportSinkProvider
{
    public ExportDestinationType Type => ExportDestinationType.AzureBlob;

    private readonly char[] _invalidBlobStartChars = new[] { '.', '/' };
    private readonly string _invalidBlobSubString = "./";
    private readonly int _folderPathMaxLength = 200;

    public IExportSink Create(IServiceProvider provider, IConfiguration config, Guid operationId)
    {
        // source objects
        var sourceClient = provider.GetRequiredService<BlobServiceClient>();
        var blobOptions = provider.GetRequiredService<IOptions<BlobOperationOptions>>();
        var blobContainerConfig = provider.GetRequiredService<IOptionsMonitor<BlobContainerConfiguration>>();

        // destination objects
        InitializeDestinationStore(config, out BlobContainerClient destClient, out string destPath);

        // init and return
        return new AzureBlobExportSink(
            sourceClient,
            destClient,
            Encoding.UTF8,
            destPath,
            $"error-{operationId:N}.log",
            blobContainerConfig,
            blobOptions);
    }

    public void Validate(IConfiguration config)
    {
        AzureBlobExportOptions options = config.Get<AzureBlobExportOptions>();

        if (options.ContainerUri == null)
            throw new FormatException();

        // Valid names https://docs.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata
        if (!string.IsNullOrWhiteSpace(options.FolderPath)
            && (_invalidBlobStartChars.Any(c => options.FolderPath.StartsWith(c))
                || options.FolderPath.Contains(_invalidBlobSubString, StringComparison.OrdinalIgnoreCase)
                || options.FolderPath.Length > _folderPathMaxLength))
        {
            throw new ExportFolderPathInvalidException(String.Format(DicomBlobResource.ExportFolderPathInvalid, options.FolderPath, string.Join(",", _invalidBlobStartChars), _folderPathMaxLength));
        }
    }

    private static void InitializeDestinationStore(IConfiguration config, out BlobContainerClient blobContainerClient, out string path)
    {
        var blobClientOptions = config.Get<BlobServiceClientOptions>();
        var exportOptions = config.Get<AzureBlobExportOptions>();

        path = exportOptions.FolderPath;

        if (exportOptions.SasToken == null)
        {
            throw new NotImplementedException();
            //need a way to pass the MI config from KeyVault to here
            //DefaultAzureCredential credential = new DefaultAzureCredential(blobClientOptions.Credentials);
            //blobContainerClient = new BlobContainerClient(exportConfig.ContainerUri, credential, blobClientOptions);
        }
        else
        {
            var builder = new UriBuilder(exportOptions.ContainerUri);
            builder.Query += exportOptions.SasToken;

            blobContainerClient = new BlobContainerClient(builder.Uri, blobClientOptions);
        }
    }
}
