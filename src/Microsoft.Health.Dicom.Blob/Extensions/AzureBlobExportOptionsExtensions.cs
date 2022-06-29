// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Blob.Extensions;

internal static class AzureBlobExportOptionsExtensions
{
    public static async Task<BlobContainerClient> GetBlobContainerClientAsync(
        this AzureBlobExportOptions exportOptions,
        IServerCredentialProvider credentialProvider,
        BlobClientOptions options,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(exportOptions, nameof(exportOptions));
        EnsureArg.IsNotNull(options, nameof(options));

        if (exportOptions.BlobContainerUri != null)
        {
            if (exportOptions.UseManagedIdentity)
            {
                TokenCredential credential = await credentialProvider.GetCredentialAsync(cancellationToken);
                if (credential == null)
                {
                    throw new InvalidOperationException(DicomBlobResource.MissingServerIdentity);
                }

                return new BlobContainerClient(exportOptions.BlobContainerUri, credential);
            }

            return new BlobContainerClient(exportOptions.BlobContainerUri);
        }

        return new BlobContainerClient(exportOptions.ConnectionString, exportOptions.BlobContainerName, options);
    }
}
