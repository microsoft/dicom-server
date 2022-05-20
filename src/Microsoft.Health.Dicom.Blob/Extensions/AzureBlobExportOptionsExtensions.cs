// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Blob.Extensions;

internal static class AzureBlobExportOptionsExtensions
{
    public static BlobContainerClient GetBlobContainerClient(this AzureBlobExportOptions exportOptions, BlobClientOptions options)
    {
        EnsureArg.IsNotNull(exportOptions, nameof(exportOptions));
        EnsureArg.IsNotNull(options, nameof(options));

        return exportOptions.ContainerUri != null
            ? new BlobContainerClient(exportOptions.ContainerUri, options)
            : new BlobContainerClient(exportOptions.ConnectionString, exportOptions.ContainerName, options);
    }
}
