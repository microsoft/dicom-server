// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

/// <summary>
/// Represents the blob container created by the service and initialized during app startup
/// </summary>
internal class InternalBlobClient : IBlobClient
{
    private readonly BlobServiceClient _client;
    private readonly string _containerName;
    public InternalBlobClient(BlobServiceClient client,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor)
    {
        _client = client;
        _containerName = namedBlobContainerConfigurationAccessor
           .Get(Constants.BlobContainerConfigurationName).ContainerName;
    }

    public bool IsExternal
    {
        get
        {
            return false;
        }
    }

    public BlobContainerClient BlobContainerClient
    {
        get
        {
            return _client.GetBlobContainerClient(_containerName);
        }
    }
}
