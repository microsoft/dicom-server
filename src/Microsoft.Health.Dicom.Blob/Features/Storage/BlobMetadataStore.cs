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
            .Get(BlobConstants.MetadataContainerConfigurationName);

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

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version);

        try
        {
            await using RecyclableMemoryStream stream = _recyclableMemoryStreamManager.GetStream(tag: nameof(StoreInstanceMetadataAsync));
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
    public async Task DeleteInstanceMetadataIfExistsAsync(long version, CancellationToken cancellationToken)
    {
        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version);

        await ExecuteAsync(t => blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, t), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DicomDataset> GetInstanceMetadataAsync(long version, CancellationToken cancellationToken)
    {
        try
        {
            BlockBlobClient blobClient = GetInstanceBlockBlobClient(version);
            return await ExecuteAsync(async t =>
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
                        "The DICOM instance metadata file with watermark '{Version}' does not exist.",
                        version);
                    break;
                case JsonException or NotSupportedException:
                    _blobRetrieveMeter.JsonDeserializationException.Add(1, new[] { new KeyValuePair<string, object>("JsonDeserializationExceptionTypeDimension", ex.GetType().FullName) });
                    break;
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteInstanceFramesRangeAsync(long version, CancellationToken cancellationToken)
    {
        BlockBlobClient blobClient = GetInstanceFramesRangeBlobClient(version);

        await ExecuteAsync(t => blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, t), cancellationToken);
    }

    /// <inheritdoc />
    public async Task StoreInstanceFramesRangeAsync(
            long version,
            IReadOnlyDictionary<int, FrameRange> framesRange,
            CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(framesRange, nameof(framesRange));

        BlockBlobClient blobClient = GetInstanceFramesRangeBlobClient(version);

        try
        {
            // TOOD: Stream directly to blob storage
            await using RecyclableMemoryStream stream = _recyclableMemoryStreamManager.GetStream(tag: nameof(StoreInstanceFramesRangeAsync));
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
    public async Task<IReadOnlyDictionary<int, FrameRange>> GetInstanceFramesRangeAsync(long version, CancellationToken cancellationToken)
    {
        BlockBlobClient cloudBlockBlob = GetInstanceFramesRangeBlobClient(version);

        try
        {
            return await ExecuteAsync(async t =>
            {
                BlobDownloadResult result = await cloudBlockBlob.DownloadContentAsync(cancellationToken);
                return result.Content.ToObjectFromJson<IReadOnlyDictionary<int, FrameRange>>(_jsonSerializerOptions);
            }, cancellationToken);
        }
        catch (ItemNotFoundException)
        {
            // With recent regression, there is a space in the blob file name, so falling back to the blob with file name if the original
            // file was not found.
            cloudBlockBlob = GetInstanceFramesRangeBlobClient(version, fallBackClient: true);
            return await ExecuteAsync(async t =>
            {
                BlobDownloadResult result = await cloudBlockBlob.DownloadContentAsync(cancellationToken);

                _logger.LogInformation("Successfully downloaded frame range metadata using fallback logic.");

                return result.Content.ToObjectFromJson<IReadOnlyDictionary<int, FrameRange>>(_jsonSerializerOptions);
            }, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DoesFrameRangeExistAsync(long version, CancellationToken cancellationToken)
    {
        BlockBlobClient blobClient = GetInstanceFramesRangeBlobClient(version);

        return await ExecuteAsync(async t =>
        {
            Response<bool> response = await blobClient.ExistsAsync(cancellationToken);
            return response.Value;
        }, cancellationToken);
    }

    private BlockBlobClient GetInstanceFramesRangeBlobClient(long version, bool fallBackClient = false)
    {
        var blobName = fallBackClient ? _nameWithPrefix.GetInstanceFramesRangeFileNameWithSpace(version) : _nameWithPrefix.GetInstanceFramesRangeFileName(version);
        return _container.GetBlockBlobClient(blobName);
    }

    private BlockBlobClient GetInstanceBlockBlobClient(long version)
    {
        string blobName = _nameWithPrefix.GetMetadataFileName(version);

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
