// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Blob.Extensions;

internal static class AzureBlobExportOptionsExtensions
{
    public static async Task<BlobContainerClient> GetBlobContainerClientAsync(
        this AzureBlobExportOptions exportOptions,
        IExportIdentityProvider identityProvider,
        BlobClientOptions options,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(exportOptions, nameof(exportOptions));
        EnsureArg.IsNotNull(options, nameof(options));

        if (exportOptions.BlobContainerUri != null)
            return exportOptions.UseManagedIdentity
                ? new BlobContainerClient(exportOptions.BlobContainerUri, await identityProvider.GetCredentialAsync(cancellationToken))
                : new BlobContainerClient(exportOptions.BlobContainerUri);

        return new BlobContainerClient(exportOptions.ConnectionString, exportOptions.BlobContainerName, options);
    }
}
