// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs;
using EnsureThat;
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

    public InternalBlobClient(BlobServiceClient blobServiceClient,
        IOptionsMonitor<BlobContainerConfiguration> optionsMonitor)
    {
        _client = EnsureArg.IsNotNull(blobServiceClient, nameof(blobServiceClient));
        _containerName = EnsureArg.IsNotNull(optionsMonitor.Get(Constants.BlobContainerConfigurationName).ContainerName, nameof(optionsMonitor));
    }

    public bool IsExternal => false;

    public BlobContainerClient BlobContainerClient => _client.GetBlobContainerClient(_containerName);

    public string GetServiceStorePath(string partitionName)
        => string.Empty;
}
