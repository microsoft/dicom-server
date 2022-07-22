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

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

/// <summary>
/// Provides functionality for managing the DICOM files using the Azure Blob storage.
/// </summary>
public class BlobFileStore : IFileStore
{
    private readonly BlobContainerClient _container;
    private readonly BlobOperationOptions _options;
    private readonly BlobMigrationFormatType _blobMigrationFormatType;
    private readonly DicomFileNameWithUid _nameWithUid;
    private readonly DicomFileNameWithPrefix _nameWithPrefix;

    public BlobFileStore(
        BlobServiceClient client,
        DicomFileNameWithUid nameWithUid,
        DicomFileNameWithPrefix nameWithPrefix,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        IOptions<BlobOperationOptions> options,
        IOptions<BlobMigrationConfiguration> blobMigrationFormatConfiguration)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(nameWithUid, nameof(nameWithUid));
        EnsureArg.IsNotNull(nameWithPrefix, nameof(nameWithPrefix));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        EnsureArg.IsNotNull(options?.Value, nameof(options));
        EnsureArg.IsNotNull(blobMigrationFormatConfiguration, nameof(blobMigrationFormatConfiguration));

        BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor
            .Get(Constants.BlobContainerConfigurationName);

        _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
        _options = options.Value;
        _nameWithUid = nameWithUid;
        _nameWithPrefix = nameWithPrefix;
        _blobMigrationFormatType = blobMigrationFormatConfiguration.Value.FormatType;
    }

    /// <inheritdoc />
    public async Task<Uri> StoreFileAsync(
        VersionedInstanceIdentifier versionedInstanceIdentifier,
        Stream stream,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
        EnsureArg.IsNotNull(stream, nameof(stream));

        BlockBlobClient[] blobClients = GetInstanceBlockBlobClients(versionedInstanceIdentifier);

        var blobUploadOptions = new BlobUploadOptions { TransferOptions = _options.Upload };

        try
        {
            foreach (var blob in blobClients)
            {
                stream.Seek(0, SeekOrigin.Begin);
                await blob.UploadAsync(stream, blobUploadOptions, cancellationToken);
            }

            return blobClients[0].Uri;
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

        BlockBlobClient[] blobClients = GetInstanceBlockBlobClients(versionedInstanceIdentifier);

        await Task.WhenAll(blobClients.Select(blob => ExecuteAsync(() => blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, cancellationToken))));
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileAsync(
        VersionedInstanceIdentifier versionedInstanceIdentifier,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(versionedInstanceIdentifier, _blobMigrationFormatType);

        Stream stream = null;
        var blobOpenReadOptions = new BlobOpenReadOptions(allowModifications: false);

        await ExecuteAsync(async () =>
        {
            // todo: RetrieableStream is returned with no Stream.Length implement which will throw when parsing using fo-dicom for transcoding and frame retrievel.
            // We should either remove fo-dicom parsing for transcoding or make SDK change to support Length property on RetriebleStream
            //Response<BlobDownloadStreamingResult> result = await blobClient.DownloadStreamingAsync(range: default, conditions: null, rangeGetContentHash: false, cancellationToken);
            //stream = result.Value.Content;
            stream = await blobClient.OpenReadAsync(blobOpenReadOptions, cancellationToken);
        });

        return stream;
    }

    public async Task<Stream> GetStreamingFileAsync(
        VersionedInstanceIdentifier versionedInstanceIdentifier,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(versionedInstanceIdentifier, _blobMigrationFormatType);

        Stream stream = null;
        var blobOpenReadOptions = new BlobOpenReadOptions(allowModifications: false);

        await ExecuteAsync(async () =>
        {
            Response<BlobDownloadStreamingResult> result = await blobClient.DownloadStreamingAsync(range: default, conditions: null, rangeGetContentHash: false, cancellationToken);
            stream = result.Value.Content;
        });

        return stream;
    }

    public async Task<FileProperties> GetFilePropertiesAsync(
        VersionedInstanceIdentifier versionedInstanceIdentifier,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(versionedInstanceIdentifier, _blobMigrationFormatType);
        FileProperties fileProperties = null;

        await ExecuteAsync(async () =>
        {
            var response = await blobClient.GetPropertiesAsync(conditions: null, cancellationToken);
            fileProperties = response.Value.ToFileProperties();
        });

        return fileProperties;
    }

    /// <inheritdoc/>
    public async Task CopyFileAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        var blobClient = GetInstanceBlockBlobClient(versionedInstanceIdentifier, BlobMigrationFormatType.Old);
        var copyBlobClient = GetInstanceBlockBlobClient(versionedInstanceIdentifier, BlobMigrationFormatType.New);

        if (!await copyBlobClient.ExistsAsync(cancellationToken))
        {
            var operation = await copyBlobClient.StartCopyFromUriAsync(blobClient.Uri, options: null, cancellationToken);
            await operation.WaitForCompletionAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileFrameAsync(
        VersionedInstanceIdentifier versionedInstanceIdentifier,
        FrameRange range,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
        EnsureArg.IsNotNull(range, nameof(range));

        BlockBlobClient blob = GetInstanceBlockBlobClient(versionedInstanceIdentifier, _blobMigrationFormatType);

        Stream stream = null;
        var blobOpenReadOptions = new BlobOpenReadOptions(allowModifications: false);

        await ExecuteAsync(async () =>
        {
            var httpRange = new HttpRange(range.Offset, range.Length);
            Response<BlobDownloadStreamingResult> result = await blob.DownloadStreamingAsync(httpRange, conditions: null, rangeGetContentHash: false, cancellationToken);
            stream = result.Value.Content;
        });
        return stream;
    }


    private BlockBlobClient GetInstanceBlockBlobClient(VersionedInstanceIdentifier versionedInstanceIdentifier, BlobMigrationFormatType formatType)
    {
        string blobName;
        if (formatType == BlobMigrationFormatType.New)
        {
            blobName = _nameWithPrefix.GetInstanceFileName(versionedInstanceIdentifier);
        }
        else
        {
            blobName = _nameWithUid.GetInstanceFileName(versionedInstanceIdentifier);
        }

        return _container.GetBlockBlobClient(blobName);
    }

    // TODO: This should removed once we migrate everything and the global flag is turned on
    private BlockBlobClient[] GetInstanceBlockBlobClients(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        var clients = new List<BlockBlobClient>(2);

        string blobName;

        if (_blobMigrationFormatType == BlobMigrationFormatType.New)
        {
            blobName = _nameWithPrefix.GetInstanceFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));
        }
        else if (_blobMigrationFormatType == BlobMigrationFormatType.Dual)
        {
            blobName = _nameWithUid.GetInstanceFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));

            blobName = _nameWithPrefix.GetInstanceFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));
        }
        else
        {
            blobName = _nameWithUid.GetInstanceFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));
        }

        return clients.ToArray();
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
