// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

/// <summary>
/// Provides functionality for managing the DICOM files using the Azure Blob storage.
/// </summary>
public class BlobFileStore : IFileStore
{
    private readonly BlobContainerClient _container;
    private readonly BlobOperationOptions _options;
    private readonly bool _dualWrite;
    private readonly InstanceNameFromUid _instanceNameFromUid;
    private readonly InstanceNameFromWatermark _instanceNameFromWatermark;

    public BlobFileStore(
        BlobServiceClient client,        
        InstanceNameFromUid instanceNameFromUid,
        InstanceNameFromWatermark instanceNameFromWatermark,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        IOptions<BlobOperationOptions> options,
        IOptions<FeatureConfiguration> featureConfiguration)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        EnsureArg.IsNotNull(options?.Value, nameof(options));
        EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
        EnsureArg.IsNotNull(blobUploader, nameof(blobUploader));

        BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor
            .Get(Constants.BlobContainerConfigurationName);

        _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
        _options = options.Value;
        _dualWrite = featureConfiguration.Value.EnableDualWrite;
    }

    /// <inheritdoc />
    public async Task<Uri> StoreFileAsync(
        VersionedInstanceIdentifier versionedInstanceIdentifier,
        Stream stream,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
        EnsureArg.IsNotNull(stream, nameof(stream));

        BlockBlobClient[] blobs = GetInstanceBlockBlobs(versionedInstanceIdentifier);
        stream.Seek(0, SeekOrigin.Begin);

        var blobUploadOptions = new BlobUploadOptions { TransferOptions = _options.Upload };

        try
        {
            await Task.WhenAll(blobs.Select(blob => blob.UploadAsync(stream, blobUploadOptions, cancellationToken)));
            return blobs[0].Uri;
        }
        catch (Exception ex)
        {
            throw new DataStoreException(ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteFileIfExistsAsync(
        VersionedInstanceIdentifier versionedInstanceIdentifier,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        BlockBlobClient blob = GetInstanceBlockBlob(versionedInstanceIdentifier);

        await ExecuteAsync(() => blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileAsync(
        VersionedInstanceIdentifier versionedInstanceIdentifier,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        BlockBlobClient blob = GetInstanceBlockBlob(versionedInstanceIdentifier);

        Stream stream = null;
        var blobOpenReadOptions = new BlobOpenReadOptions(allowModifications: false);

        await ExecuteAsync(async () =>
        {
            stream = await blob.OpenReadAsync(blobOpenReadOptions, cancellationToken);
        });

        return stream;
    }

    public async Task<FileProperties> GetFilePropertiesAsync(
        VersionedInstanceIdentifier versionedInstanceIdentifier,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        BlockBlobClient blob = GetInstanceBlockBlob(versionedInstanceIdentifier);
        FileProperties fileProperties = null;

        await ExecuteAsync(async () =>
        {
            var response = await blob.GetPropertiesAsync(conditions: null, cancellationToken);
            fileProperties = response.Value.ToFileProperties();
        });

        return fileProperties;
    }

    // TODO: get rid of this
    private BlockBlobClient GetInstanceBlockBlob(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        return GetInstanceBlockBlobs(versionedInstanceIdentifier)[0];
    }

    private BlockBlobClient[] GetInstanceBlockBlobs(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        List<string> blobNames = new List<string>();
        blobNames.Add(_instanceNameFromUid.GetInstanceFileName(versionedInstanceIdentifier));
        if (_dualWrite)
        {
            blobNames.Add(_instanceNameFromWatermark.GetInstanceFileName(versionedInstanceIdentifier));
        }
        BlockBlobClient[] clients = new BlockBlobClient[blobNames.Count];
        for (int i = 0; i < clients.Length; i++)
        {
            clients[i] = _container.GetBlockBlobClient(blobNames[i]);
        }

        return clients;
    }

    private static async Task ExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
        {
            throw new ItemNotFoundException(ex);
        }
        catch (Exception ex)
        {
            throw new DataStoreException(ex);
        }
    }
}
