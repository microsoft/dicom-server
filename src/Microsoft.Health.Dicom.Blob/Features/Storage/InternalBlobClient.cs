// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

/// <summary>
/// Represents the blob container created by the service and initialized during app startup
/// </summary>
internal class InternalBlobClient : IBlobClient
{
    private readonly BlobServiceClient _client;
    private readonly string _containerName;
    private readonly ILogger _logger;

    public InternalBlobClient(
        BlobServiceClient blobServiceClient,
        IOptionsMonitor<BlobContainerConfiguration> optionsMonitor,
        ILogger<InternalBlobClient> logger)
    {
        _client = EnsureArg.IsNotNull(blobServiceClient, nameof(blobServiceClient));
        _containerName = EnsureArg.IsNotNull(optionsMonitor.Get(BlobConstants.BlobContainerConfigurationName).ContainerName, nameof(optionsMonitor));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _logger.LogInformation("Internal blob client registered.");
    }

    public bool IsExternal => false;

    public BlobContainerClient BlobContainerClient => _client.GetBlobContainerClient(_containerName);

    public string GetServiceStorePath(string partitionName)
        => string.Empty;

    public BlobRequestConditions GetConditions(FileProperties fileProperties) => null;
}
