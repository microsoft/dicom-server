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
    public Task<FileProperties> StoreFileAsync(
        long version,
        string partitionName,
        Stream stream,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));

        BlockBlobClient blobClient = GetNewInstanceBlockBlobClient(version, partitionName);

        var blobUploadOptions = new BlobUploadOptions { TransferOptions = _options.Upload };
        stream.Seek(0, SeekOrigin.Begin);

        return ExecuteAsync(
            func: async () =>
            {
                BlobContentInfo info = await blobClient.UploadAsync(stream, blobUploadOptions, cancellationToken);
                return new FileProperties
                {
                    Path = blobClient.Name,
                    ETag = info.ETag.ToString(),
                    ContentLength = stream.Length,
                };
            },
            operationName: nameof(StoreFileAsync),
            operationType: OperationType.Input,
            extractLength: long? (newBlobFileProperties) => newBlobFileProperties.ContentLength);
    }

    /// <inheritdoc />
    public Task<FileProperties> StoreFileInBlocksAsync(
        long version,
        Partition partition,
        Stream stream,
        IDictionary<string, long> blockLengths,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));
        EnsureArg.IsNotNull(partition, nameof(partition));
        EnsureArg.IsNotNull(blockLengths, nameof(blockLengths));
        EnsureArg.IsGte(blockLengths.Count, 0, nameof(blockLengths.Count));

        BlockBlobClient blobClient = GetNewInstanceBlockBlobClient(version, partition.Name);

        int maxBufferSize = (int)blockLengths.Max(x => x.Value);

        return ExecuteAsync(
            func: async () =>
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(maxBufferSize);
                FileProperties fileProperties;
                try
                {
                    foreach ((string blockId, long blockSize) in blockLengths)
                    {
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
                        await stream.ReadAsync(buffer, 0, (int)blockSize, cancellationToken);
#pragma warning restore CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'

                        using var blockStream = new MemoryStream(buffer, 0, (int)blockSize);
                        await blobClient.StageBlockAsync(blockId, blockStream, cancellationToken: cancellationToken);
                    }

                    BlobContentInfo info = await blobClient.CommitBlockListAsync(
                        blockLengths.Keys,
                        cancellationToken: cancellationToken);

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
            },
            operationName: nameof(StoreFileInBlocksAsync),
            operationType: OperationType.Input,
            extractLength: long? (newBlobFileProperties) => newBlobFileProperties.ContentLength);
    }

    /// <inheritdoc />
    public Task<FileProperties> UpdateFileBlockAsync(
        long version,
        Partition partition,
        FileProperties fileProperties,
        string blockId,
        Stream stream,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));
        EnsureArg.IsNotNull(partition, nameof(partition));
        EnsureArg.IsNotNullOrWhiteSpace(blockId, nameof(blockId));

        return ExecuteAsync(
            func: async () =>
            {
                BlockBlobClient blobClient = GetExistingInstanceBlockBlobClient(version, partition, fileProperties);
                _logger.LogInformation(
                    "Trying to read block list for DICOM instance file with version '{Version}'.",
                    version);

                BlockList blockList = await blobClient.GetBlockListAsync(
                    BlockListTypes.Committed,
                    snapshot: null,
                    conditions: null, // GetBlockListAsync does not support IfMatch conditions to check eTag
                    cancellationToken);

                IEnumerable<string> blockIds = blockList.CommittedBlocks.Select(x => x.Name);

                string blockToUpdate = blockIds.FirstOrDefault(x => x.Equals(blockId, StringComparison.OrdinalIgnoreCase));

                if (blockToUpdate == null)
                    throw new DataStoreException(DicomBlobResource.BlockNotFound, null, _blobClient.IsExternal);

                stream.Seek(0, SeekOrigin.Begin);
                await blobClient.StageBlockAsync(blockId, stream, cancellationToken: cancellationToken);
                BlobContentInfo info = await blobClient.CommitBlockListAsync(
                    blockIds,
                    cancellationToken: cancellationToken);
                return new FileProperties
                {
                    Path = blobClient.Name,
                    ETag = info.ETag.ToString(),
                    ContentLength = stream.Length
                };
            },
            operationName: nameof(UpdateFileBlockAsync),
            operationType: OperationType.Input,
            extractLength: long? (newBlobFileProperties) => newBlobFileProperties.ContentLength);
    }

    /// <inheritdoc />
    public async Task DeleteFileIfExistsAsync(
        long version,
        Partition partition,
        FileProperties fileProperties,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(partition);

        BlockBlobClient blobClient = GetExistingInstanceBlockBlobClient(version, partition, fileProperties);
        _logger.LogInformation(
            "Trying to delete DICOM instance file with watermark: '{Version}' and PartitionKey: {PartitionKey}.",
            version,
            partition.Key);

        await ExecuteAsync(
            func: async () =>
            {
                try
                {
                    // NOTE - when file does not exist but conditions passed in, it fails on conditions not met
                    return await blobClient.DeleteIfExistsAsync(
                        DeleteSnapshotsOption.IncludeSnapshots,
                        conditions: _blobClient.GetConditions(fileProperties),
                        cancellationToken);
                }
                catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.ConditionNotMet && _blobClient.IsExternal)
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
                        version,
                        partition.Key);
                }

                return null;
            },
            operationName: nameof(DeleteFileIfExistsAsync),
            operationType: OperationType.Input);
    }

    /// <inheritdoc />
    public Task<Stream> GetFileAsync(
        long version,
        Partition partition,
        FileProperties fileProperties,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(partition, nameof(partition));

        BlockBlobClient blobClient = GetExistingInstanceBlockBlobClient(version, partition, fileProperties);

        var blobOpenReadOptions = new BlobOpenReadOptions(allowModifications: false);
        blobOpenReadOptions.Conditions = _blobClient.GetConditions(fileProperties);
        _logger.LogInformation("Trying to read DICOM instance file with watermark '{Version}'.", version);
        // todo: RetrievableStream is returned with no Stream.Length implement which will throw when parsing using fo-dicom for transcoding and frame retrieved.
        // We should either remove fo-dicom parsing for transcoding or make SDK change to support Length property on RetrievableStream
        //Response<BlobDownloadStreamingResult> result = await blobClient.DownloadStreamingAsync(range: default, conditions: null, rangeGetContentHash: false, cancellationToken);
        //stream = result.Value.Content;
        return ExecuteAsync(
            func: () => blobClient.OpenReadAsync(blobOpenReadOptions, cancellationToken),
            operationName: nameof(GetFileAsync),
            operationType: OperationType.Output,
            extractLength: long? (stream) => stream.Length);
    }

    /// <inheritdoc />
    public async Task<Stream> GetStreamingFileAsync(
        long version,
        Partition partition,
        FileProperties fileProperties,
        CancellationToken cancellationToken)
    {
        BlockBlobClient blobClient = GetExistingInstanceBlockBlobClient(version, partition, fileProperties);

        _logger.LogInformation("Trying to read DICOM instance file with watermark '{Version}'.", version);

        BlobDownloadStreamingResult result = await ExecuteAsync(
            func: async () =>
            {
                Response<BlobDownloadStreamingResult> result = await blobClient.DownloadStreamingAsync(
                    range: default,
                    conditions: _blobClient.GetConditions(fileProperties),
                    rangeGetContentHash: false,
                    cancellationToken);
                return result.Value;
            },
            operationName: nameof(GetStreamingFileAsync),
            operationType: OperationType.Output,
            extractLength: long? (result) => result.Details.ContentLength);

        return result.Content;
    }

    /// <inheritdoc />
    public Task<FileProperties> GetFilePropertiesAsync(
        long version,
        Partition partition,
        FileProperties fileProperties,
        CancellationToken cancellationToken)
    {
        BlockBlobClient blobClient = GetExistingInstanceBlockBlobClient(version, partition, fileProperties);
        _logger.LogInformation("Trying to read DICOM instance fileProperties with watermark '{Version}'.", version);

        return ExecuteAsync(
            func: async () =>
            {
                BlobProperties blobProperties = await blobClient.GetPropertiesAsync(
                    conditions: _blobClient.GetConditions(fileProperties),
                    cancellationToken);

                return new FileProperties
                {
                    Path = blobClient.Name,
                    ETag = blobProperties.ETag.ToString(),
                    ContentLength = blobProperties.ContentLength,
                };
            },
            operationName: nameof(GetFilePropertiesAsync),
            operationType: OperationType.Output);
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileFrameAsync(
        long version,
        Partition partition,
        FrameRange range,
        FileProperties fileProperties,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(range, nameof(range));

        BlockBlobClient blob = GetExistingInstanceBlockBlobClient(version, partition, fileProperties);
        _logger.LogInformation(
            "Trying to read DICOM instance file with version '{Version}' on range {Offset}-{Length}.",
            version,
            range.Offset,
            range.Length);

        BlobDownloadStreamingResult result = await ExecuteAsync(
            func: async () =>
            {
                Response<BlobDownloadStreamingResult> result = await blob.DownloadStreamingAsync(
                    range: new HttpRange(range.Offset, range.Length),
                    conditions: _blobClient.GetConditions(fileProperties),
                    rangeGetContentHash: false,
                    cancellationToken);
                return result.Value;
            },
            operationName: nameof(GetFileFrameAsync),
            operationType: OperationType.Output,
            extractLength: long? (result) => result.Details.ContentLength);

        return result.Content;
    }

    /// <inheritdoc />
    public async Task<BinaryData> GetFileContentInRangeAsync(
        long version,
        Partition partition,
        FileProperties fileProperties,
        FrameRange range,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(range, nameof(range));
        EnsureArg.IsNotNull(partition, nameof(partition));
        BlockBlobClient blob = GetExistingInstanceBlockBlobClient(version, partition, fileProperties);
        _logger.LogInformation(
            "Trying to read DICOM instance fileContent with version '{Version}' on range {Offset}-{Length}.",
            version,
            range.Offset,
            range.Length);

        var blobDownloadOptions = new BlobDownloadOptions
        {
            Range = new HttpRange(range.Offset, range.Length),
            Conditions = _blobClient.GetConditions(fileProperties),
        };

        BlobDownloadResult result = await ExecuteAsync(
            func: async () =>
            {
                Response<BlobDownloadResult> result = await blob.DownloadContentAsync(blobDownloadOptions, cancellationToken);
                return result.Value;
            },
            operationName: nameof(GetFileContentInRangeAsync),
            operationType: OperationType.Output,
            extractLength: long? (result) => result.Details.ContentLength);

        return result.Content;
    }

    /// <inheritdoc />
    public Task<KeyValuePair<string, long>> GetFirstBlockPropertyAsync(
        long version,
        Partition partition,
        FileProperties fileProperties,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(partition, nameof(partition));
        BlockBlobClient blobClient = GetExistingInstanceBlockBlobClient(version, partition, fileProperties);
        _logger.LogInformation("Trying to read DICOM instance file with version '{Version}' firstBlock.", version);

        return ExecuteAsync(
            func: async () =>
            {
                BlockList blockList = await blobClient.GetBlockListAsync(
                    BlockListTypes.Committed,
                    snapshot: null,
                    conditions: null, // GetBlockListAsync does not support IfMatch conditions to check eTag
                    cancellationToken);

                if (!blockList.CommittedBlocks.Any())
                    throw new DataStoreException(DicomBlobResource.BlockListNotFound, null, _blobClient.IsExternal);

                BlobBlock firstBlock = blockList.CommittedBlocks.First();
                return new KeyValuePair<string, long>(firstBlock.Name, firstBlock.Size);
            },
            operationName: nameof(GetFirstBlockPropertyAsync),
            operationType: OperationType.Output);
    }

    /// <inheritdoc />
    public Task CopyFileAsync(
        long originalVersion,
        long newVersion,
        Partition partition,
        FileProperties fileProperties,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(partition, nameof(partition));
        var blobClient = GetExistingInstanceBlockBlobClient(originalVersion, partition, fileProperties);
        var copyBlobClient = GetNewInstanceBlockBlobClient(newVersion, partition.Name);
        _logger.LogInformation(
            "Trying to copy DICOM instance file from original version '{Version}' to new path with new version'{NewVersion}'.",
            originalVersion,
            newVersion);

        return ExecuteAsync(
            func: async () =>
            {
                BlobCopyFromUriOptions options = new BlobCopyFromUriOptions();
                options.SourceConditions = _blobClient.GetConditions(fileProperties);

                if (!await copyBlobClient.ExistsAsync(cancellationToken))
                {
                    _logger.LogInformation(
                        "Operation {OperationName} processed within CopyFileAsync.",
                        "ExistsAsync");
                    var operation = await copyBlobClient.StartCopyFromUriAsync(blobClient.Uri, options: options, cancellationToken);
                    await operation.WaitForCompletionAsync(cancellationToken);
                    return true;
                }

                return false;
            },
            operationName: nameof(CopyFileAsync),
            operationType: OperationType.Input);
    }

    /// <inheritdoc />
    public Task SetBlobToColdAccessTierAsync(
        long version,
        Partition partition,
        FileProperties fileProperties,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(partition, nameof(partition));
        BlockBlobClient blobClient = GetExistingInstanceBlockBlobClient(version, partition, fileProperties);
        _logger.LogInformation("Trying to set blob tier for DICOM instance file with watermark '{Version}'.", version);

        return ExecuteAsync(
            func: () => blobClient.SetAccessTierAsync(
                AccessTier.Cold,
                conditions: null, // SetAccessTierAsync does not support matching on etag
                cancellationToken: cancellationToken),
            operationName: nameof(SetBlobToColdAccessTierAsync),
            operationType: OperationType.Input);
    }

    /// <summary>
    /// Gets client based on watermark/version and partition.
    /// </summary>
    /// <param name="version">Version of file to get</param>
    /// <param name="partitionName">Partition within which the file should live in</param>
    /// <remarks>Do not use for any *existing* file. Only use for new files which may not already have file properties
    /// associated with them.</remarks>
    protected virtual BlockBlobClient GetNewInstanceBlockBlobClient(long version, string partitionName)
    {
        string blobName = _nameWithPrefix.GetInstanceFileName(version);
        string fullPath = _blobClient.GetServiceStorePath(partitionName) + blobName;
        // does not throw, just appends uri with blobName
        return _blobClient.BlobContainerClient.GetBlockBlobClient(fullPath);
    }

    /// <summary>
    /// Get client based on watermark/version and partition or file properties. 
    /// </summary>
    /// <param name="version">Version of file to get</param>
    /// <param name="partition">Partition within which the file should live in</param>
    /// <param name="fileProperties">File properties to use for external store. If not using external store, set to null.</param>
    /// <remarks>Always use on existing files whether using for external or internal store.</remarks>
    protected virtual BlockBlobClient GetExistingInstanceBlockBlobClient(
        long version,
        Partition partition,
        FileProperties fileProperties)
    {
        EnsureArg.IsNotNull(partition, nameof(partition));
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

    private async Task<T> ExecuteAsync<T>(
        Func<Task<T>> func,
        string operationName,
        OperationType operationType,
        Func<T, long?> extractLength = null)
    {
        try
        {
            var resp = await func();
            EmitTelemetry(nameof(operationName), operationType, extractLength?.Invoke(resp));
            return resp;
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

    private void EmitTelemetry(string operationName, OperationType operationType, long? streamLength = null)
    {
        _blobFileStoreMeter.BlobFileStoreOperationCount.Add(
            1,
            BlobFileStoreMeter.CreateTelemetryDimension(operationName, operationType, _blobClient.IsExternal));

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
                BlobFileStoreMeter.CreateTelemetryDimension(
                    operationName,
                    operationType,
                    _blobClient.IsExternal));
        }
    }
}