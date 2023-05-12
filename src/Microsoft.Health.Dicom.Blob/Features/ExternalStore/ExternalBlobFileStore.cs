// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Utilities;

namespace Microsoft.Health.Dicom.Blob.Features.ExternalStore;

internal class ExternalBlobFileStore : BlobFileStore
{
    private readonly ExternalBlobDataStoreConfiguration _externalStoreOptions;

    public ExternalBlobFileStore(
        IBlobClient blobClient,
        DicomFileNameWithPrefix nameWithPrefix,
        IOptions<BlobOperationOptions> options,
        ILogger<BlobFileStore> logger,
        IOptions<ExternalBlobDataStoreConfiguration> externalStoreOptions) :
        base(blobClient, nameWithPrefix, options, logger)
    {
        _externalStoreOptions = EnsureArg.IsNotNull(externalStoreOptions?.Value, nameof(externalStoreOptions));
    }
    private protected override BlockBlobClient GetInstanceBlockBlobClient(long version)
    {
        var filePath = _externalStoreOptions.ServiceStorePath;
        string blobName = _nameWithPrefix.GetInstanceFileName(version);

        var fullPath = filePath + blobName;

        // does not throw, just appends uri with blobName
        return _blobClient.BlobContainerClient.GetBlockBlobClient(fullPath);
    }
}
