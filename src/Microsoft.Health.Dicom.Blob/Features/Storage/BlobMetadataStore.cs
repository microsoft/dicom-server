// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.Health.Dicom.Blob.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.IO;
using NotSupportedException = System.NotSupportedException;

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
    private readonly DicomFileNameWithPrefix _nameWithPrefix;
    private readonly ILogger<BlobMetadataStore> _logger;
    private readonly BlobStoreMeter _blobStoreMeter;
    private readonly BlobRetrieveMeter _blobRetrieveMeter;

    public BlobMetadataStore(
        BlobServiceClient client,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        DicomFileNameWithPrefix nameWithPrefix,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        IOptions<JsonSerializerOptions> jsonSerializerOptions,
        BlobStoreMeter blobStoreMeter,
        BlobRetrieveMeter blobRetrieveMeter,
        ILogger<BlobMetadataStore> logger)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        _jsonSerializerOptions = EnsureArg.IsNotNull(jsonSerializerOptions?.Value, nameof(jsonSerializerOptions));
        _nameWithPrefix = EnsureArg.IsNotNull(nameWithPrefix, nameof(nameWithPrefix));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        _recyclableMemoryStreamManager = EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _blobStoreMeter = EnsureArg.IsNotNull(blobStoreMeter, nameof(blobStoreMeter));
        _blobRetrieveMeter = EnsureArg.IsNotNull(blobRetrieveMeter, nameof(blobRetrieveMeter));

        BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor
            .Get(Constants.MetadataContainerConfigurationName);

        _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
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

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(dicomDatasetWithoutBulkData.ToVersionedInstanceIdentifier(version));

        try
        {
            await using Stream stream = _recyclableMemoryStreamManager.GetStream(StoreInstanceMetadataStreamTagName);
            await JsonSerializer.SerializeAsync(stream, dicomDatasetWithoutBulkData, _jsonSerializerOptions, cancellationToken);


            stream.Seek(0, SeekOrigin.Begin);
            await blobClient.UploadAsync(
                stream,
                new BlobHttpHeaders { ContentType = KnownContentTypes.ApplicationJsonUtf8 },
                metadata: null,
                conditions: null,
                accessTier: null,
                progressHandler: null,
                cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex is NotSupportedException)
            {
                _blobStoreMeter.JsonSerializationException.Add(1, new[] { new KeyValuePair<string, object>("ExceptionType", ex.GetType().FullName) });
            }
            throw new DataStoreException(ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteInstanceMetadataIfExistsAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(versionedInstanceIdentifier);

        await ExecuteAsync(t => blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, t), cancellationToken);
    }

    /// <inheritdoc />
    public Task<DicomDataset> GetInstanceMetadataAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        try
        {
            BlockBlobClient blobClient = GetInstanceBlockBlobClient(versionedInstanceIdentifier);
            return ExecuteAsync(async t =>
            {
                // TODO: When the JsonConverter for DicomDataset does not need to Seek, we can use DownloadStreaming instead
                BlobDownloadResult result = await blobClient.DownloadContentAsync(t);

                // DICOM metadata file includes UTF-8 encoding with BOM and there is a bug with the
                // BinaryData.ToObjectFromJson method as seen in this issue: https://github.com/dotnet/runtime/issues/71447
                return await JsonSerializer.DeserializeAsync<DicomDataset>(result.Content.ToStream(), _jsonSerializerOptions, t);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case ItemNotFoundException:
                    _logger.LogWarning(
                        ex,
                        "The DICOM instance metadata file with '{DicomInstanceIdentifier}' does not exist.",
                        versionedInstanceIdentifier);
                    break;
                case JsonException or NotSupportedException:
                    _blobRetrieveMeter.JsonDeserializationException.Add(1, new[] { new KeyValuePair<string, object>("JsonDeserializationExceptionTypeDimension", ex.GetType().FullName) });
                    break;
            }

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
            // TOOD: Stream directly to blob storage
            await using Stream stream = _recyclableMemoryStreamManager.GetStream(StoreInstanceFramesRangeTagName);
            await JsonSerializer.SerializeAsync(stream, framesRange, _jsonSerializerOptions, cancellationToken);

            stream.Seek(0, SeekOrigin.Begin);
            await blobClient.UploadAsync(
                stream,
                new BlobHttpHeaders { ContentType = KnownContentTypes.ApplicationJsonUtf8 },
                metadata: null,
                conditions: null,
                accessTier: null,
                progressHandler: null,
                cancellationToken);
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

    private BlockBlobClient GetInstanceFramesRangeBlobClient(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        var blobName = DicomFileNameWithPrefix.GetInstanceFramesRangeFileName(versionedInstanceIdentifier);
        return _container.GetBlockBlobClient(blobName);
    }

    private BlockBlobClient GetInstanceBlockBlobClient(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        string blobName = _nameWithPrefix.GetMetadataFileName(versionedInstanceIdentifier);

        return _container.GetBlockBlobClient(blobName);
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
