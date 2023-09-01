// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Core;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Health.Core.Features.Identity;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Blob.Extensions;

internal static class AzureBlobExportOptionsExtensions
{
    public static BlobContainerClient GetBlobContainerClient(
        this AzureBlobExportOptions exportOptions,
        IExternalCredentialProvider credentialProvider,
        BlobClientOptions options)
    {
        EnsureArg.IsNotNull(exportOptions, nameof(exportOptions));
        EnsureArg.IsNotNull(options, nameof(options));

        if (exportOptions.BlobContainerUri != null)
        {
            if (exportOptions.UseManagedIdentity)
            {
                TokenCredential credential = credentialProvider.GetTokenCredential();
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
