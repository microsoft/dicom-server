// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;
public class BlobCopyStore : IFileCopyStore
{
    private readonly BlobContainerClient _sourceContainer;
    private readonly BlobContainerClient _destBlobContainerClient;
    private readonly string _destinationPath;

    public BlobCopyStore(
        BlobServiceClient client,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        BlobContainerClient destBlobContainerClient,
        string destinationPath)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        EnsureArg.IsNotNull(destBlobContainerClient, nameof(destBlobContainerClient));

        BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor
            .Get(Constants.BlobContainerConfigurationName);

        _sourceContainer = client.GetBlobContainerClient(containerConfiguration.ContainerName);
        _destBlobContainerClient = destBlobContainerClient;
        _destinationPath = destinationPath;
    }

    public async Task CopyFileAsync(VersionedInstanceIdentifier instanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));

        // Init destination blob
        string destBlobName = GenerateDestinationBlobName(_destinationPath, instanceIdentifier);
        var destBlobClient = _destBlobContainerClient.GetBlobClient(destBlobName);

        // Init source blob
        var srcBlobClient = GetInstanceBlockBlob(instanceIdentifier);
        // todo config setting for token expiration time
        Uri sourceUri = srcBlobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.Now.AddHours(1));

        // Start the copy operation.
        CopyFromUriOperation operation = await destBlobClient.StartCopyFromUriAsync(sourceUri, options: null, cancellationToken);
        Response<long> azureResponse = await operation.WaitForCompletionAsync(cancellationToken);

        if (azureResponse.GetRawResponse().IsError)
        {
            throw new DataStoreCopyFailedException(azureResponse.GetRawResponse().ToString());
        }
    }

    private BlobClient GetInstanceBlockBlob(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        string blobName = $"{versionedInstanceIdentifier.StudyInstanceUid}/{versionedInstanceIdentifier.SeriesInstanceUid}/{versionedInstanceIdentifier.SopInstanceUid}_{versionedInstanceIdentifier.Version}.dcm";
        return _sourceContainer.GetBlobClient(blobName);
    }

    private static string GenerateDestinationBlobName(string destinationPath, VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        return $"{destinationPath}/{versionedInstanceIdentifier.StudyInstanceUid}/{versionedInstanceIdentifier.SeriesInstanceUid}/{versionedInstanceIdentifier.SopInstanceUid}.dcm";
    }
}
