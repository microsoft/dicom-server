// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Features.Telemetry;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Diagnostic;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

/// <summary>
/// Provides functionality for managing the DICOM files using the Azure Blob storage.
/// </summary>
public class BlobFileStore : IFileStore
{
    private readonly BlobOperationOptions _options;
    private readonly ILogger<BlobFileStore> _logger;
    private readonly IBlobClient _blobClient;
    private readonly DicomFileNameWithPrefix _nameWithPrefix;
    private readonly BlobFileStoreMeter _blobFileStoreMeter;
    private readonly TelemetryClient _telemetryClient;

    private static readonly Action<ILogger, string, bool, Exception> LogBlobClientOperationDelegate =
        LoggerMessage.Define<string, bool>(
            LogLevel.Information,
            default,
            "Operation '{OperationName}' processed. Using external store {IsExternalStore}.");

    private static readonly Action<ILogger, string, long, Exception> LogBlobClientOperationWithStreamDelegate =
        LoggerMessage.Define<string, long>(
            LogLevel.Information,
            default,
            "Operation '{OperationName}' processed stream length '{StreamLength}'.");

    public BlobFileStore(
        IBlobClient blobClient,
        DicomFileNameWithPrefix nameWithPrefix,
        IOptions<BlobOperationOptions> options,
        BlobFileStoreMeter blobFileStoreMeter,
        ILogger<BlobFileStore> logger,
        TelemetryClient telemetryClient)
    {
        _nameWithPrefix = EnsureArg.IsNotNull(nameWithPrefix, nameof(nameWithPrefix));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _blobClient = EnsureArg.IsNotNull(blobClient, nameof(blobClient));
        _blobFileStoreMeter = EnsureArg.IsNotNull(blobFileStoreMeter, nameof(blobFileStoreMeter));
        _telemetryClient = EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
    }

    /// <inheritdoc />
    public async Task<FileProperties> StoreFileAsync(
        long version,
        string partitionName,
        Stream stream,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(stream, nameof(stream)); // do we want to discard here?

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partitionName);

        BlobUploadOptions
            blobUploadOptions = new BlobUploadOptions
                { TransferOptions = _options.Upload }; // use explicit type instead of var, good
        // wants to simplify this new expression, I don't like it' suggestion as much
        // new() { TransferOptions = _options.Upload }; VS new BlobUploadOptions { TransferOptions = _options.Upload };
        stream.Seek(0, SeekOrigin.Begin); // again, wants to use discard

        BlobContentInfo info = await ExecuteAsync<BlobContentInfo>(async () =>
            await blobClient.UploadAsync(stream, blobUploadOptions,
                cancellationToken)); // wants us to call configure await

        EmitTelemetry(nameof(StoreFileAsync), OperationType.Input, stream.Length);

        return new FileProperties
        {
            Path = blobClient.Name,
            ETag = info.ETag.ToString(),
            ContentLength = stream.Length,
        };
    }

    /// <inheritdoc />
    public async Task<FileProperties> StoreFileInBlocksAsync(
        long version,
        Partition partition,
        Stream stream,
        IDictionary<string, long> blockLengths,
        CancellationToken cancellationToken)
    {
        _ = EnsureArg.IsNotNull(stream, nameof(stream));
        _ = EnsureArg.IsNotNull(partition, nameof(partition));
        _ = EnsureArg.IsNotNull(blockLengths, nameof(blockLengths));
        _ = EnsureArg.IsGte(blockLengths.Count, 0, nameof(blockLengths.Count));

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partition.Name);

        int maxBufferSize = (int)blockLengths.Max(x => x.Value);

        return await ExecuteAsync(async () => // configure await appended at bottom
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(maxBufferSize);
            FileProperties fileProperties = null;
            try
            {
                foreach ((string blockId, long blockSize) in blockLengths)
                {
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
                    _ = await stream.ReadAsync(buffer, 0, (int)blockSize, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'

                    using MemoryStream blockStream = new(buffer, 0, (int)blockSize);
                    _ = await blobClient.StageBlockAsync(blockId, blockStream, cancellationToken: cancellationToken)
                        .ConfigureAwait(false); // wants discard
                }

                BlobContentInfo info = await blobClient
                    .CommitBlockListAsync(blockLengths.Keys, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                EmitTelemetry(nameof(StoreFileInBlocksAsync), OperationType.Input, stream.Length);

                fileProperties = new FileProperties
                {
                    Path = blobClient.Name,
                    ETag = info.ETag.ToString(),
                    ContentLength = stream.Length,
                };
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return fileProperties;
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FileProperties> UpdateFileBlockAsync(
        long version,
        Partition partition,
        FileProperties fileProperties,
        string blockId,
        Stream stream,
        CancellationToken cancellationToken)
    {
        _ = EnsureArg.IsNotNull(stream, nameof(stream));
        _ = EnsureArg.IsNotNull(partition, nameof(partition));
        _ = EnsureArg.IsNotNullOrWhiteSpace(blockId, nameof(blockId));

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partition, fileProperties);
        _logger.LogInformation("Trying to read block list for DICOM instance file with version '{Version}'.",
            version); // wants logger message delegate

        BlockList blockList = await ExecuteAsync<BlockList>(async () => await blobClient.GetBlockListAsync(
            BlockListTypes.Committed,
            snapshot: null,
            conditions: null, // GetBlockListAsync does not support IfMatch conditions to check eTag
            cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        IEnumerable<string> blockIds = blockList.CommittedBlocks.Select(x => x.Name);

        string blockToUpdate = blockIds.FirstOrDefault(x => x.Equals(blockId, StringComparison.OrdinalIgnoreCase));

        if (blockToUpdate == null)
            throw new DataStoreException(DicomBlobResource.BlockNotFound, null,
                _blobClient.IsExternal); // want to coalesce if expression
        // if allow coalesce, it would coalesce above two statements to, but I don't think this is as readable.
        // string blockToUpdate = blockIds.FirstOrDefault(x => x.Equals(blockId, StringComparison.OrdinalIgnoreCase)) ?? throw new DataStoreException(DicomBlobResource.BlockNotFound, null, _blobClient.IsExternal);


        _ = stream.Seek(0, SeekOrigin.Begin);

        BlobContentInfo info = await ExecuteAsync(async () =>
        {
            _ = await blobClient.StageBlockAsync(blockId, stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return await blobClient.CommitBlockListAsync(blockIds, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }).ConfigureAwait(false);

        EmitTelemetry(nameof(UpdateFileBlockAsync), OperationType.Input, stream.Length);

        return new FileProperties
        {
            Path = blobClient.Name,
            ETag = info.ETag.ToString(),
            ContentLength = stream.Length
        };
    }

    /// <inheritdoc />
    public async Task DeleteFileIfExistsAsync(long version, Partition partition, FileProperties fileProperties,
        CancellationToken cancellationToken)
    {
        _ = EnsureArg.IsNotNull(partition);

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partition, fileProperties);
        _logger.LogInformation(
            "Trying to delete DICOM instance file with watermark: '{Version}' and PartitionKey: {PartitionKey}.",
            version, partition.Key); // wants delegate

        EmitTelemetry(nameof(DeleteFileIfExistsAsync), OperationType.Input);

        _ = await ExecuteAsync(async () =>
        {
            try
            {
                // NOTE - when file does not exist but conditions passed in, it fails on conditions not met
                return await blobClient.DeleteIfExistsAsync(
                    DeleteSnapshotsOption.IncludeSnapshots,
                    conditions: _blobClient.GetConditions(fileProperties),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.ConditionNotMet &&
                                                    _blobClient.IsExternal)
            {
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.ExternalDataStoreBlobModified,
                    ex.ErrorCode,
                    "delete",
                    fileProperties.Path,
                    fileProperties.ETag);

                _telemetryClient.ForwardLogTrace(message, partition, fileProperties);

                _logger.LogInformation(
                    "Can not delete blob in external store as it has changed or been deleted. File from watermark: '{Version}' and PartitionKey: {PartitionKey}. Dangling SQL Index detected. Will not retry",
                    version, partition.Key);
            }

            return null;
        }).ConfigureAwait(false);
    }

    private void EmitTelemetry(string operationName, OperationType operationType, long? streamLength = null)
    {
        _blobFileStoreMeter.BlobFileStoreOperationCount.Add(
            1,
            BlobFileStoreMeter.CreateBlobFileStoreOperationTelemetryDimension(operationName, operationType,
                _blobClient.IsExternal));

        if (streamLength == null)
        {
            LogBlobClientOperationDelegate(_logger, operationName, _blobClient.IsExternal, null);
        }
        else
        {
            var length = streamLength.Value;
            LogBlobClientOperationWithStreamDelegate(_logger, operationName, streamLength.Value, null);
            _blobFileStoreMeter.BlobFileStoreOperationStreamSize.Add(
                length,
                BlobFileStoreMeter.CreateBlobFileStoreOperationTelemetryDimension(operationName, operationType,
                    _blobClient.IsExternal));
        }
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileAsync(long version, Partition partition, FileProperties fileProperties,
        CancellationToken cancellationToken)
    {
        _ = EnsureArg.IsNotNull(partition, nameof(partition));

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partition, fileProperties);

        BlobOpenReadOptions blobOpenReadOptions = new(allowModifications: false) // simplified object initialization
        {
            Conditions = _blobClient.GetConditions(fileProperties)
        };
        _logger.LogInformation("Trying to read DICOM instance file with watermark '{Version}'.", version);
        // todo: RetrievableStream is returned with no Stream.Length implement which will throw when parsing using fo-dicom for transcoding and frame retrieved.
        // We should either remove fo-dicom parsing for transcoding or make SDK change to support Length property on RetrievableStream
        //Response<BlobDownloadStreamingResult> result = await blobClient.DownloadStreamingAsync(range: default, conditions: null, rangeGetContentHash: false, cancellationToken);
        //stream = result.Value.Content;
        return await ExecuteAsync(async () =>
        {
            Stream stream = await blobClient.OpenReadAsync(blobOpenReadOptions, cancellationToken)
                .ConfigureAwait(false);
            EmitTelemetry(nameof(GetFileAsync), OperationType.Output, stream.Length);
            return stream;
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Stream> GetStreamingFileAsync(long version, string partitionName,
        CancellationToken cancellationToken)
    {
        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partitionName);

        _logger.LogInformation("Trying to read DICOM instance file with watermark '{Version}'.", version);

        return await ExecuteAsync(async () =>
        {
            Response<BlobDownloadStreamingResult> result = await blobClient
                .DownloadStreamingAsync(range: default, conditions: null, rangeGetContentHash: false, cancellationToken)
                .ConfigureAwait(false);

            EmitTelemetry(nameof(GetStreamingFileAsync), OperationType.Output, result.Value.Details.ContentLength);

            return result.Value.Content;
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FileProperties> GetFilePropertiesAsync(long version, string partitionName,
        CancellationToken cancellationToken)
    {
        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partitionName);
        _logger.LogInformation("Trying to read DICOM instance fileProperties with watermark '{Version}'.", version);

        return await ExecuteAsync(async () =>
        {
            BlobProperties blobProperties = await blobClient.GetPropertiesAsync(conditions: null, cancellationToken)
                .ConfigureAwait(false);

            EmitTelemetry(nameof(GetFilePropertiesAsync), OperationType.Output);

            return new FileProperties
            {
                Path = blobClient.Name,
                ETag = blobProperties.ETag.ToString(),
                ContentLength = blobProperties.ContentLength,
            };
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileFrameAsync(long version, string partitionName, FrameRange range,
        CancellationToken cancellationToken)
    {
        _ = EnsureArg.IsNotNull(range, nameof(range));

        BlockBlobClient blob = GetInstanceBlockBlobClient(version, partitionName);
        _logger.LogInformation(
            "Trying to read DICOM instance file with version '{Version}' on range {Offset}-{Length}.", version,
            range.Offset, range.Length);

        return await ExecuteAsync(async () =>
        {
            HttpRange httpRange = new HttpRange(range.Offset, range.Length);
            Response<BlobDownloadStreamingResult> result = await blob
                .DownloadStreamingAsync(httpRange, conditions: null, rangeGetContentHash: false, cancellationToken)
                .ConfigureAwait(false);

            EmitTelemetry(nameof(GetFileFrameAsync), OperationType.Output, result.Value.Details.ContentLength);

            return result.Value.Content;
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BinaryData> GetFileContentInRangeAsync(long version, Partition partition,
        FileProperties fileProperties, FrameRange range, CancellationToken cancellationToken)
    {
        _ = EnsureArg.IsNotNull(range, nameof(range));
        _ = EnsureArg.IsNotNull(partition, nameof(partition));
        BlockBlobClient blob = GetInstanceBlockBlobClient(version, partition, fileProperties);
        _logger.LogInformation(
            "Trying to read DICOM instance fileContent with version '{Version}' on range {Offset}-{Length}.", version,
            range.Offset, range.Length);

        BlobDownloadOptions blobDownloadOptions = new()
        {
            Range = new HttpRange(range.Offset, range.Length),
            Conditions = _blobClient.GetConditions(fileProperties),
        };

        return await ExecuteAsync(async () =>
        {
            Response<BlobDownloadResult> result =
                await blob.DownloadContentAsync(blobDownloadOptions, cancellationToken).ConfigureAwait(false);

            EmitTelemetry(nameof(GetFileContentInRangeAsync), OperationType.Output, result.Value.Details.ContentLength);

            return result.Value.Content;
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<KeyValuePair<string, long>> GetFirstBlockPropertyAsync(long version, Partition partition,
        FileProperties fileProperties, CancellationToken cancellationToken = default)
    {
        _ = EnsureArg.IsNotNull(partition, nameof(partition));
        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partition, fileProperties);
        _logger.LogInformation("Trying to read DICOM instance file with version '{Version}' firstBlock.", version);

        return await ExecuteAsync(async () =>
        {
            BlockList blockList = await blobClient.GetBlockListAsync(
                BlockListTypes.Committed,
                snapshot: null,
                conditions: null, // GetBlockListAsync does not support IfMatch conditions to check eTag
                cancellationToken).ConfigureAwait(false);

            if (!blockList.CommittedBlocks.Any())
                throw new DataStoreException(DicomBlobResource.BlockListNotFound, null, _blobClient.IsExternal);

            BlobBlock firstBlock = blockList.CommittedBlocks.First();

            EmitTelemetry(nameof(GetFirstBlockPropertyAsync), OperationType.Output);
            return new KeyValuePair<string, long>(firstBlock.Name, firstBlock.Size);
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CopyFileAsync(long originalVersion, long newVersion, Partition partition,
        FileProperties fileProperties, CancellationToken cancellationToken)
    {
        _ = EnsureArg.IsNotNull(partition, nameof(partition));
        BlockBlobClient blobClient = GetInstanceBlockBlobClient(originalVersion, partition, fileProperties);
        BlockBlobClient copyBlobClient = GetInstanceBlockBlobClient(newVersion, partition.Name);
        _logger.LogInformation(
            "Trying to copy DICOM instance file from original version '{Version}' to new path with new version'{NewVersion}'.",
            originalVersion, newVersion);

        _ = await ExecuteAsync(async () =>
        {
            BlobCopyFromUriOptions options = new()
            {
                SourceConditions = _blobClient.GetConditions(fileProperties)
            };

            if (!await copyBlobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation(
                    "Operation {OperationName} processed within CopyFileAsync.",
                    "ExistsAsync");
                CopyFromUriOperation operation = await copyBlobClient
                    .StartCopyFromUriAsync(blobClient.Uri, options: options, cancellationToken).ConfigureAwait(false);
                _ = await operation.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);

                EmitTelemetry(nameof(CopyFileAsync), OperationType.Input);
                return true;
            }

            return false;
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetBlobToColdAccessTierAsync(long version, Partition partition, FileProperties fileProperties,
        CancellationToken cancellationToken = default)
    {
        _ = EnsureArg.IsNotNull(partition, nameof(partition));
        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partition, fileProperties);
        _logger.LogInformation("Trying to set blob tier for DICOM instance file with watermark '{Version}'.", version);

        _ = await ExecuteAsync(async () =>
        {
            // SetAccessTierAsync does not support matching on etag
            Response response = await blobClient
                .SetAccessTierAsync(AccessTier.Cold, conditions: null, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            EmitTelemetry(nameof(SetBlobToColdAccessTierAsync), OperationType.Input);
            return response;
        }).ConfigureAwait(false);
    }

    protected virtual BlockBlobClient GetInstanceBlockBlobClient(long version, string partitionName)
    {
        string blobName = _nameWithPrefix.GetInstanceFileName(version);
        string fullPath = _blobClient.GetServiceStorePath(partitionName) + blobName;
        // does not throw, just appends uri with blobName
        return _blobClient.BlobContainerClient.GetBlockBlobClient(fullPath);
    }

    protected virtual BlockBlobClient GetInstanceBlockBlobClient(long version, Partition partition,
        FileProperties fileProperties)
    {
        _ = EnsureArg.IsNotNull(partition, nameof(partition));
        if (_blobClient.IsExternal && fileProperties != null)
        {
            // does not throw, just appends uri with blobName
            return _blobClient.BlobContainerClient.GetBlockBlobClient(fileProperties.Path);
        }

        string blobName = _nameWithPrefix.GetInstanceFileName(version);
        string fullPath = _blobClient.GetServiceStorePath(partition.Name) + blobName;
        // does not throw, just appends uri with blobName
        return _blobClient.BlobContainerClient.GetBlockBlobClient(fullPath);
    }

    private async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound && !_blobClient.IsExternal)
        {
            _logger.LogError(ex, message: "Access to storage account failed with ErrorCode: {ErrorCode}", ex.ErrorCode);
            throw new ItemNotFoundException(ex);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, message: "Access to storage account failed with ErrorCode: {ErrorCode}", ex.ErrorCode);
            throw new DataStoreRequestFailedException(ex, _blobClient.IsExternal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Access to storage account failed");
            throw new DataStoreException(ex, _blobClient.IsExternal);
        }
    }
}