// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

/// <summary>
/// Provides functionality for managing the DICOM instance metadata.
/// </summary>
public class BlobMetadataStore : IMetadataStore
{
    private const string StoreInstanceMetadataStreamTagName = nameof(BlobMetadataStore) + "." + nameof(StoreInstanceMetadataAsync);
    private const string StoreInstanceFramesRangeTagName = nameof(BlobMetadataStore) + "." + nameof(StoreInstanceFramesRangeAsync);
    private readonly BlobContainerClient _container;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly BlobMigrationFormatType _blobMigrationFormatType;
    private readonly DicomFileNameWithUid _nameWithUid;
    private readonly DicomFileNameWithPrefix _nameWithPrefix;
    private readonly ILogger<BlobMetadataStore> _logger;

    public BlobMetadataStore(
        BlobServiceClient client,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        DicomFileNameWithUid fileNameWithUid,
        DicomFileNameWithPrefix nameWithPrefix,
        IOptions<BlobMigrationConfiguration> blobMigrationFormatConfiguration,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        IOptions<JsonSerializerOptions> jsonSerializerOptions,
        ILogger<BlobMetadataStore> logger)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        _jsonSerializerOptions = EnsureArg.IsNotNull(jsonSerializerOptions?.Value, nameof(jsonSerializerOptions));
        _nameWithUid = EnsureArg.IsNotNull(fileNameWithUid, nameof(fileNameWithUid));
        _nameWithPrefix = EnsureArg.IsNotNull(nameWithPrefix, nameof(nameWithPrefix));
        EnsureArg.IsNotNull(blobMigrationFormatConfiguration, nameof(blobMigrationFormatConfiguration));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        _recyclableMemoryStreamManager = EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));

        BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor
            .Get(Constants.MetadataContainerConfigurationName);

        _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
        _blobMigrationFormatType = blobMigrationFormatConfiguration.Value.FormatType;
    }

    /// <inheritdoc />
    public async Task StoreInstanceMetadataAsync(
        DicomDataset dicomDataset,
        long version,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        // Creates a copy of the dataset with bulk data removed.
        DicomDataset dicomDatasetWithoutBulkData = dicomDataset.CopyWithoutBulkDataItems();

        BlockBlobClient[] blobClients = GetInstanceBlockBlobClients(dicomDatasetWithoutBulkData.ToVersionedInstanceIdentifier(version));

        try
        {
            await using (Stream stream = _recyclableMemoryStreamManager.GetStream(StoreInstanceMetadataStreamTagName))
            await using (Utf8JsonWriter utf8Writer = new Utf8JsonWriter(stream))
            {
                // TODO: Use SerializeAsync in .NET 6
                JsonSerializer.Serialize(utf8Writer, dicomDatasetWithoutBulkData, _jsonSerializerOptions);
                await utf8Writer.FlushAsync(cancellationToken);

                foreach (var blob in blobClients)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.UploadAsync(
                        stream,
                        new BlobHttpHeaders { ContentType = KnownContentTypes.ApplicationJson },
                        metadata: null,
                        conditions: null,
                        accessTier: null,
                        progressHandler: null,
                        cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            throw new DataStoreException(ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteInstanceMetadataIfExistsAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        BlockBlobClient[] blobClients = GetInstanceBlockBlobClients(versionedInstanceIdentifier);

        await Task.WhenAll(blobClients.Select(blob => ExecuteAsync(t => blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, t), cancellationToken)));
    }

    /// <inheritdoc />
    public Task<DicomDataset> GetInstanceMetadataAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        try
        {
            BlockBlobClient blobClient = GetInstanceBlockBlobClient(versionedInstanceIdentifier, _blobMigrationFormatType);
            return ExecuteAsync(async t =>
            {
                BlobDownloadResult result = await blobClient.DownloadContentAsync(t);

                // DICOM metadata file includes UTF-8 encoding with BOM and there is a bug with the BinaryData.ToObjectFromJson method as seen in this issue: https://github.com/dotnet/runtime/issues/71447
                // So passing as stream which will remove the UTF-8 BOM.
                return await JsonSerializer.DeserializeAsync<DicomDataset>(result.Content.ToStream(), _jsonSerializerOptions, t);
            }, cancellationToken);
        }
        catch (ItemNotFoundException ex)
        {
            _logger.LogWarning(ex, "The DICOM instance metadata file with '{DicomInstanceIdentifier}' does not exist.", versionedInstanceIdentifier);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteInstanceFramesRangeAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
        BlockBlobClient blobClient = GetInstanceFramesRangeBlobClient(versionedInstanceIdentifier);

        await ExecuteAsync(t => blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, t), cancellationToken);
    }

    /// <inheritdoc />
    public async Task StoreInstanceFramesRangeAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            IReadOnlyDictionary<int, FrameRange> framesRange,
            CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
        EnsureArg.IsNotNull(framesRange, nameof(framesRange));

        BlockBlobClient blobClient = GetInstanceFramesRangeBlobClient(versionedInstanceIdentifier);

        try
        {
            await using (Stream stream = _recyclableMemoryStreamManager.GetStream(StoreInstanceFramesRangeTagName))
            await using (Utf8JsonWriter utf8Writer = new Utf8JsonWriter(stream))
            {
                JsonSerializer.Serialize(utf8Writer, framesRange, _jsonSerializerOptions);
                await utf8Writer.FlushAsync(cancellationToken);
                stream.Seek(0, SeekOrigin.Begin);

                await blobClient.UploadAsync(
                    stream,
                    new BlobHttpHeaders()
                    {
                        ContentType = KnownContentTypes.ApplicationJson,
                    },
                    metadata: null,
                    conditions: null,
                    accessTier: null,
                    progressHandler: null,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new DataStoreException(ex);
        }

    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<int, FrameRange>> GetInstanceFramesRangeAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
        BlockBlobClient cloudBlockBlob = GetInstanceFramesRangeBlobClient(versionedInstanceIdentifier);

        return ExecuteAsync(async t =>
        {
            BlobDownloadResult result = await cloudBlockBlob.DownloadContentAsync(cancellationToken);
            return result.Content.ToObjectFromJson<IReadOnlyDictionary<int, FrameRange>>(_jsonSerializerOptions);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CopyInstanceMetadataAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
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

    private BlockBlobClient GetInstanceFramesRangeBlobClient(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        var blobName = DicomFileNameWithPrefix.GetInstanceFramesRangeFileName(versionedInstanceIdentifier);
        return _container.GetBlockBlobClient(blobName);
    }

    private BlockBlobClient GetInstanceBlockBlobClient(VersionedInstanceIdentifier versionedInstanceIdentifier, BlobMigrationFormatType formatType)
    {
        string blobName;
        if (formatType == BlobMigrationFormatType.New)
        {
            blobName = _nameWithPrefix.GetMetadataFileName(versionedInstanceIdentifier);
        }
        else
        {
            blobName = _nameWithUid.GetMetadataFileName(versionedInstanceIdentifier);
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
            blobName = _nameWithPrefix.GetMetadataFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));
        }
        else if (_blobMigrationFormatType == BlobMigrationFormatType.Dual)
        {
            blobName = _nameWithUid.GetMetadataFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));

            blobName = _nameWithPrefix.GetMetadataFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));
        }
        else
        {
            blobName = _nameWithUid.GetMetadataFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));
        }

        return clients.ToArray();
    }

    private static async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        try
        {
            return await action(cancellationToken);
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
