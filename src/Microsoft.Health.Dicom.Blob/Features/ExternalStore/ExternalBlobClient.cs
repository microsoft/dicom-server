// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


using Azure;
using Azure.Storage.Blobs;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Dicom.Blob.Features.ExternalStore;
internal class ExternalBlobClient : IBlobClient
{
    private readonly object _lockObj = new object();
    private BlobContainerClient _azureBlobServiceClient;
    private readonly ExternalBlobDataStoreConfiguration _options;

    public ExternalBlobClient(
        IOptions<ExternalBlobDataStoreConfiguration> options)
    {
        _options = options.Value;
    }

    public bool IsExternal
    {
        get
        {
            return true;
        }
    }

    public BlobContainerClient BlobContainerClient
    {
        get
        {
            if (_azureBlobServiceClient == null)
            {
                lock (_lockObj)
                {
                    if (_azureBlobServiceClient == null)
                    {
                        // todo use external MI
                        // IExternalOperationCredentialProvider credentialProvider
                        //TokenCredential credential = credentialProvider.GetTokenCredential();
                        //return new BlobContainerClient(exportOptions.BlobContainerUri, credential);

                        try
                        {
                            _azureBlobServiceClient = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
                        }
                        catch (RequestFailedException ex)
                        {
                            throw new DataStoreException(ex, isExternal: true);
                        }
                    }

                }
            }
            return _azureBlobServiceClient;
        }
    }
}
