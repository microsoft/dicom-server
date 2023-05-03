// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


using Azure.Core;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Exceptions;
using System;

namespace Microsoft.Health.Dicom.Blob.Features.ExternalStore;

/// Represents the blob container created by the user and initialized JIT
internal class ExternalBlobClient : IBlobClient
{
    private readonly object _lockObj = new object();
    private readonly BlobServiceClientOptions _blobClientOptions;
    private readonly ExternalBlobDataStoreConfiguration _externalStoreOptions;
    private readonly IExternalOperationCredentialProvider _credentialProvider;
    private BlobContainerClient _blobContainerClient;

    public ExternalBlobClient(
        IExternalOperationCredentialProvider credentialProvider,
        IOptions<ExternalBlobDataStoreConfiguration> externalStoreOptions,
        IOptions<BlobServiceClientOptions> blobClientOptions)
    {
        _credentialProvider = EnsureArg.IsNotNull(credentialProvider, nameof(credentialProvider));
        _blobClientOptions = EnsureArg.IsNotNull(blobClientOptions?.Value, nameof(blobClientOptions));
        _externalStoreOptions = EnsureArg.IsNotNull(externalStoreOptions?.Value, nameof(externalStoreOptions));
    }

    public bool IsExternal => true;

    public BlobContainerClient BlobContainerClient
    {
        get
        {
            if (_blobContainerClient == null)
            {
                lock (_lockObj)
                {
                    if (_blobContainerClient == null)
                    {
                        try
                        {
                            if (_externalStoreOptions.BlobContainerUri != null)
                            {
                                TokenCredential credential = _credentialProvider.GetTokenCredential();
                                _blobContainerClient = new BlobContainerClient(_externalStoreOptions.BlobContainerUri, credential, _blobClientOptions);
                            }
                            else
                            {
                                _blobContainerClient = new BlobContainerClient(_externalStoreOptions.ConnectionString, _externalStoreOptions.ContainerName, _blobClientOptions);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new DataStoreException(ex, isExternal: IsExternal);
                        }
                    }
                }
            }
            return _blobContainerClient;
        }
    }
}
