// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
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
    private readonly UidAsInstanceName _uidAsInstanceName;
    private readonly WatermarkAsInstanceName _watermarkAsInstanceName;

    public BlobFileStore(
        BlobServiceClient client,
        UidAsInstanceName uidAsInstanceName,
        WatermarkAsInstanceName watermarkAsInstanceName,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        IOptions<BlobOperationOptions> options)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        EnsureArg.IsNotNull(options?.Value, nameof(options));

        BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor
            .Get(Constants.BlobContainerConfigurationName);

        _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
        _options = options.Value;
        _uidAsInstanceName = EnsureArg.IsNotNull(uidAsInstanceName, nameof(uidAsInstanceName));
        _watermarkAsInstanceName = EnsureArg.IsNotNull(watermarkAsInstanceName, nameof(watermarkAsInstanceName));
    }

    /// <inheritdoc />
    public async Task<Uri> StoreFileAsync(
        VersionedInstanceIdentifier versionedInstanceIdentifier,
        Stream stream,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
        EnsureArg.IsNotNull(stream, nameof(stream));

        BlockBlobClient blob = GetInstanceBlockBlob(versionedInstanceIdentifier);
        stream.Seek(0, SeekOrigin.Begin);

        var blobUploadOptions = new BlobUploadOptions { TransferOptions = _options.Upload };

        try
        {
            await blob.UploadAsync(
                stream,
                blobUploadOptions,
                cancellationToken);

            return blob.Uri;
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

    private BlockBlobClient GetInstanceBlockBlob(VersionedInstanceIdentifier versionedInstanceIdentifier, bool duplicated = false)
    {
        IInstanceNameBuilder nameBuilder = duplicated ? _watermarkAsInstanceName : _uidAsInstanceName;
        string blobName = nameBuilder.GetInstanceFileName(versionedInstanceIdentifier);
        return _container.GetBlockBlobClient(blobName);
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

    public async Task DuplicateFileAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
    {
        var blob = GetInstanceBlockBlob(versionedInstanceIdentifier, duplicated: false);
        var duplicatedBlob = GetInstanceBlockBlob(versionedInstanceIdentifier, duplicated: true);
        if (!await duplicatedBlob.ExistsAsync(cancellationToken))
        {
            var operation = await duplicatedBlob.StartCopyFromUriAsync(blob.Uri, options: null, cancellationToken);
            await operation.WaitForCompletionAsync(cancellationToken);
        }
    }
}
