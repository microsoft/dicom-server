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
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;
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
    private readonly BlobMigrationFormatType _blobMigrationFormatType;
    private readonly bool _logOldFormatUsage;
    private readonly DicomFileNameWithUid _nameWithUid;
    private readonly DicomFileNameWithPrefix _nameWithPrefix;
    private readonly ILogger<BlobMetadataStore> _logger;
    private readonly TelemetryClient _telemetryClient;

    public BlobMetadataStore(
        BlobServiceClient client,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        DicomFileNameWithUid fileNameWithUid,
        DicomFileNameWithPrefix nameWithPrefix,
        IOptions<BlobMigrationConfiguration> blobMigrationFormatConfiguration,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        IOptions<JsonSerializerOptions> jsonSerializerOptions,
        ILogger<BlobMetadataStore> logger,
        TelemetryClient telemetryClient)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        _jsonSerializerOptions = EnsureArg.IsNotNull(jsonSerializerOptions?.Value, nameof(jsonSerializerOptions));
        _nameWithUid = EnsureArg.IsNotNull(fileNameWithUid, nameof(fileNameWithUid));
        _nameWithPrefix = EnsureArg.IsNotNull(nameWithPrefix, nameof(nameWithPrefix));
        EnsureArg.IsNotNull(blobMigrationFormatConfiguration, nameof(blobMigrationFormatConfiguration));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        _recyclableMemoryStreamManager = EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _telemetryClient = EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));

        BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor
            .Get(Constants.MetadataContainerConfigurationName);

        _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
        _blobMigrationFormatType = blobMigrationFormatConfiguration.Value.FormatType;
        _logOldFormatUsage = blobMigrationFormatConfiguration.Value.LogOldFormatUsage;
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
            await using Stream stream = _recyclableMemoryStreamManager.GetStream(StoreInstanceMetadataStreamTagName);
            await JsonSerializer.SerializeAsync(stream, dicomDatasetWithoutBulkData, _jsonSerializerOptions, cancellationToken);

            foreach (BlockBlobClient blob in blobClients)
            {
                stream.Seek(0, SeekOrigin.Begin);
                await blob.UploadAsync(
                    stream,
                    new BlobHttpHeaders { ContentType = KnownContentTypes.ApplicationJsonUtf8 },
                    metadata: null,
                    conditions: null,
                    accessTier: null,
                    progressHandler: null,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            if (ex is NotSupportedException)
            {
                _telemetryClient
                    .GetMetric("JsonSerializationException", "ExceptionType")
                    .TrackValue(1, ex.GetType().FullName);
            }
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
                    _telemetryClient
                        .GetMetric("JsonDeserializationException", "ExceptionType")
                        .TrackValue(1, ex.GetType().FullName);
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

    /// <inheritdoc />
    public async Task DeleteOldInstanceMetadataIfExistsAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, bool forceDelete = false, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        var blobClient = GetInstanceBlockBlobClient(versionedInstanceIdentifier, BlobMigrationFormatType.Old);
        var newBlobClient = GetInstanceBlockBlobClient(versionedInstanceIdentifier, BlobMigrationFormatType.New);

        if (forceDelete || await newBlobClient.ExistsAsync(cancellationToken))
        {
            await ExecuteAsync(t => blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, t), cancellationToken);
        }
        else
        {
            throw new DataStoreException("DICOM metadata does not exists with new format.", FailureReasonCodes.BlobNotFound);
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
            LogOldFormatUsage();
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
            LogOldFormatUsage();

            blobName = _nameWithUid.GetMetadataFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));

            blobName = _nameWithPrefix.GetMetadataFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));
        }
        else
        {
            LogOldFormatUsage();

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

    private void LogOldFormatUsage()
    {
        if (_logOldFormatUsage)
        {
            _logger.LogInformation("Using old blob format.");
        }
    }
}
