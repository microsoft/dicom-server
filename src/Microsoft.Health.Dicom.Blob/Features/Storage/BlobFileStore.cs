// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;

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

    public BlobFileStore(
        IBlobClient blobClient,
        DicomFileNameWithPrefix nameWithPrefix,
        IOptions<BlobOperationOptions> options,
        ILogger<BlobFileStore> logger)
    {
        _nameWithPrefix = EnsureArg.IsNotNull(nameWithPrefix, nameof(nameWithPrefix));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _blobClient = EnsureArg.IsNotNull(blobClient, nameof(blobClient));
    }

    /// <inheritdoc />
    public async Task<FileProperties> StoreFileAsync(
        long version,
        string partitionName,
        Stream stream,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partitionName);

        var blobUploadOptions = new BlobUploadOptions { TransferOptions = _options.Upload };
        stream.Seek(0, SeekOrigin.Begin);

        BlobContentInfo info = await ExecuteAsync<BlobContentInfo>(async () => await blobClient.UploadAsync(stream, blobUploadOptions, cancellationToken));

        return new FileProperties
        {
            Path = blobClient.Name,
            ETag = info.ETag.ToString(),
            ContentLength = stream.Length,
        };
    }

    /// <inheritdoc />
    public async Task<Uri> StoreFileInBlocksAsync(
        long version,
        Stream stream,
        IDictionary<string, long> blockLengths,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));
        EnsureArg.IsNotNull(blockLengths, nameof(blockLengths));
        EnsureArg.IsGte(blockLengths.Count, 0, nameof(blockLengths.Count));

        // pass in partition name once update supported by external store #104373
        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, string.Empty);

        int maxBufferSize = (int)blockLengths.Max(x => x.Value);

        return await ExecuteAsync(async () =>
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(maxBufferSize);
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

                await blobClient.CommitBlockListAsync(blockLengths.Keys, cancellationToken: cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return blobClient.Uri;
        });
    }

    /// <inheritdoc />
    public async Task UpdateFileBlockAsync(
        long version,
        string blockId,
        Stream stream,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));
        EnsureArg.IsNotNullOrWhiteSpace(blockId, nameof(blockId));

        // pass in partition name once update supported by external store #104373
        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, string.Empty);
        _logger.LogInformation("Trying to read block list for DICOM instance file with watermark '{Version}'.", version);

        BlockList blockList = await ExecuteAsync<BlockList>(async () => await blobClient.GetBlockListAsync(
            BlockListTypes.Committed,
            snapshot: null,
            conditions: null,
            cancellationToken));

        IEnumerable<string> blockIds = blockList.CommittedBlocks.Select(x => x.Name);

        string blockToUpdate = blockIds.FirstOrDefault(x => x.Equals(blockId, StringComparison.OrdinalIgnoreCase));

        if (blockToUpdate == null)
            throw new DataStoreException(DicomBlobResource.BlockNotFound, null, _blobClient.IsExternal);

        stream.Seek(0, SeekOrigin.Begin);

        await ExecuteAsync(async () =>
        {
            await blobClient.StageBlockAsync(blockId, stream, cancellationToken: cancellationToken);
            return await blobClient.CommitBlockListAsync(blockIds, cancellationToken: cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task DeleteFileIfExistsAsync(long version, string partitionName, CancellationToken cancellationToken)
    {

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partitionName);
        _logger.LogInformation("Trying to delete DICOM instance file with watermark '{Version}'.", version);

        await ExecuteAsync(() => blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileAsync(long version, string partitionName, CancellationToken cancellationToken)
    {

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partitionName);

        var blobOpenReadOptions = new BlobOpenReadOptions(allowModifications: false);
        _logger.LogInformation("Trying to read DICOM instance file with watermark '{Version}'.", version);
        // todo: RetrievableStream is returned with no Stream.Length implement which will throw when parsing using fo-dicom for transcoding and frame retrieved.
        // We should either remove fo-dicom parsing for transcoding or make SDK change to support Length property on RetrievableStream
        //Response<BlobDownloadStreamingResult> result = await blobClient.DownloadStreamingAsync(range: default, conditions: null, rangeGetContentHash: false, cancellationToken);
        //stream = result.Value.Content;
        return await ExecuteAsync(async () => await blobClient.OpenReadAsync(blobOpenReadOptions, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Stream> GetStreamingFileAsync(long version, string partitionName, CancellationToken cancellationToken)
    {
        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partitionName);

        _logger.LogInformation("Trying to read DICOM instance file with watermark '{Version}'.", version);

        return await ExecuteAsync(async () =>
        {
            Response<BlobDownloadStreamingResult> result = await blobClient.DownloadStreamingAsync(range: default, conditions: null, rangeGetContentHash: false, cancellationToken);
            return result.Value.Content;
        });
    }

    /// <inheritdoc />
    public async Task<FileProperties> GetFilePropertiesAsync(long version, string partitionName, CancellationToken cancellationToken)
    {
        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, partitionName);
        _logger.LogInformation("Trying to read DICOM instance fileProperties with watermark '{Version}'.", version);

        return await ExecuteAsync(async () =>
        {
            BlobProperties blobProperties = await blobClient.GetPropertiesAsync(conditions: null, cancellationToken);
            return new FileProperties
            {
                Path = blobClient.Name,
                ETag = blobProperties.ETag.ToString(),
                ContentLength = blobProperties.ContentLength,
            };
        });
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileFrameAsync(long version, string partitionName, FrameRange range, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(range, nameof(range));

        BlockBlobClient blob = GetInstanceBlockBlobClient(version, partitionName);
        _logger.LogInformation("Trying to read DICOM instance file with watermark '{Version}' on range {Offset}-{Length}.", version, range.Offset, range.Length);

        return await ExecuteAsync(async () =>
        {
            var httpRange = new HttpRange(range.Offset, range.Length);
            Response<BlobDownloadStreamingResult> result = await blob.DownloadStreamingAsync(httpRange, conditions: null, rangeGetContentHash: false, cancellationToken);
            return result.Value.Content;
        });
    }

    /// <inheritdoc />
    public async Task<BinaryData> GetFileContentInRangeAsync(long version, FrameRange range, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(range, nameof(range));
        // pass in partition name once update supported by external store #104373
        BlockBlobClient blob = GetInstanceBlockBlobClient(version, string.Empty);
        _logger.LogInformation("Trying to read DICOM instance fileContent with watermark '{Version}' on range {Offset}-{Length}.", version, range.Offset, range.Length);

        var blobDownloadOptions = new BlobDownloadOptions
        {
            Range = new HttpRange(range.Offset, range.Length)
        };

        return await ExecuteAsync(async () =>
        {
            Response<BlobDownloadResult> result = await blob.DownloadContentAsync(blobDownloadOptions, cancellationToken);
            return result.Value.Content;
        });
    }

    /// <inheritdoc />
    public async Task<KeyValuePair<string, long>> GetFirstBlockPropertyAsync(long version, CancellationToken cancellationToken = default)
    {
        // pass in partition name once update supported by external store #104373
        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version, string.Empty);
        _logger.LogInformation("Trying to read DICOM instance file with watermark '{Version}' firstBlock.", version);

        return await ExecuteAsync(async () =>
        {
            BlockList blockList = await blobClient.GetBlockListAsync(BlockListTypes.Committed, snapshot: null, conditions: null, cancellationToken);

            if (!blockList.CommittedBlocks.Any())
                throw new DataStoreException(DicomBlobResource.BlockListNotFound, null, _blobClient.IsExternal);

            BlobBlock firstBlock = blockList.CommittedBlocks.First();
            return new KeyValuePair<string, long>(firstBlock.Name, firstBlock.Size);
        });
    }

    /// <inheritdoc />
    public async Task CopyFileAsync(long originalVersion, long newVersion, CancellationToken cancellationToken)
    {
        // pass in partition name once update supported by external store #104373
        var blobClient = GetInstanceBlockBlobClient(originalVersion, string.Empty);
        var copyBlobClient = GetInstanceBlockBlobClient(newVersion, string.Empty);
        _logger.LogInformation("Trying to copy DICOM instance file with watermark '{Version}' to new watermark '{NewVersion}.", originalVersion, newVersion);

        if (!await copyBlobClient.ExistsAsync(cancellationToken))
        {
            var operation = await copyBlobClient.StartCopyFromUriAsync(blobClient.Uri, options: null, cancellationToken);
            await operation.WaitForCompletionAsync(cancellationToken);
        }
    }

    protected virtual BlockBlobClient GetInstanceBlockBlobClient(long version, string partitionName)
    {
        string blobName = _nameWithPrefix.GetInstanceFileName(version);
        string fullPath = _blobClient.GetServiceStorePath(partitionName) + blobName;
        // does not throw, just appends uri with blobName
        return _blobClient.BlobContainerClient.GetBlockBlobClient(fullPath);
    }

    private async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
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
