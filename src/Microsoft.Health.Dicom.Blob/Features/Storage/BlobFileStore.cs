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
    private readonly BlobContainerClient _container;
    private readonly BlobOperationOptions _options;
    private readonly DicomFileNameWithPrefix _nameWithPrefix;
    private readonly ILogger<BlobFileStore> _logger;

    public BlobFileStore(
        BlobServiceClient client,
        DicomFileNameWithPrefix nameWithPrefix,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        IOptions<BlobOperationOptions> options,
        ILogger<BlobFileStore> logger)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        _nameWithPrefix = EnsureArg.IsNotNull(nameWithPrefix, nameof(nameWithPrefix));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));

        BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor
            .Get(Constants.BlobContainerConfigurationName);

        _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
    }

    /// <inheritdoc />
    public async Task<Uri> StoreFileAsync(
        long version,
        Stream stream,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version);

        var blobUploadOptions = new BlobUploadOptions { TransferOptions = _options.Upload };

        try
        {
            stream.Seek(0, SeekOrigin.Begin);
            await blobClient.UploadAsync(stream, blobUploadOptions, cancellationToken);

            return blobClient.Uri;
        }
        catch (Exception ex)
        {
            throw new DataStoreException(ex);
        }
    }

    public async Task<Uri> StoreFileInBlocksAsync(
        long version,
        Stream stream,
        int initialBlockLength,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));

        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version);

        const int stagedBlockSize = 4 * 1024 * 1024;

        try
        {
            stream.Seek(0, SeekOrigin.Begin);

            long fileSize = stream.Length - initialBlockLength;
            int numStages = (int)Math.Ceiling((double)fileSize / stagedBlockSize) + 1;
            string[] blockIds = new string[numStages];

            long bytesRead = 0;
            for (int i = 0; i < numStages; i++)
            {
                long blockSize = i == 0 ? initialBlockLength : Math.Min(stagedBlockSize, stream.Length - bytesRead);

                byte[] blockData = new byte[blockSize];

                await stream.ReadAsync(blockData.AsMemory(0, (int)blockSize), cancellationToken);

                string blockId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

                using (MemoryStream blockStream = new MemoryStream(blockData))
                {
                    await blobClient.StageBlockAsync(blockId, blockStream, cancellationToken: cancellationToken);
                }

                blockIds[i] = blockId;
                bytesRead += blockSize;
            }

            await blobClient.CommitBlockListAsync(blockIds, cancellationToken: cancellationToken);

            return blobClient.Uri;
        }
        catch (Exception ex)
        {
            throw new DataStoreException(ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteFileIfExistsAsync(long version, CancellationToken cancellationToken)
    {
        try
        {
            BlockBlobClient blobClient = GetInstanceBlockBlobClient(version);

            await ExecuteAsync(() => blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, cancellationToken));
        }
        catch (Exception ex)
        {
            throw new DataStoreException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileAsync(long version, CancellationToken cancellationToken)
    {
        try
        {
            BlockBlobClient blobClient = GetInstanceBlockBlobClient(version);

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
        catch (ItemNotFoundException ex)
        {
            _logger.LogWarning(ex, "The DICOM instance file with watermark '{Version}' does not exist.", version);

            throw;
        }
    }

    public async Task<Stream> GetStreamingFileAsync(long version, CancellationToken cancellationToken)
    {
        BlockBlobClient blobClient = GetInstanceBlockBlobClient(version);

        Stream stream = null;
        var blobOpenReadOptions = new BlobOpenReadOptions(allowModifications: false);

        await ExecuteAsync(async () =>
        {
            Response<BlobDownloadStreamingResult> result = await blobClient.DownloadStreamingAsync(range: default, conditions: null, rangeGetContentHash: false, cancellationToken);
            stream = result.Value.Content;
        });

        return stream;
    }

    public async Task<FileProperties> GetFilePropertiesAsync(long version, CancellationToken cancellationToken)
    {
        try
        {
            BlockBlobClient blobClient = GetInstanceBlockBlobClient(version);
            FileProperties fileProperties = null;

            await ExecuteAsync(async () =>
            {
                var response = await blobClient.GetPropertiesAsync(conditions: null, cancellationToken);
                fileProperties = response.Value.ToFileProperties();
            });

            return fileProperties;
        }
        catch (ItemNotFoundException ex)
        {
            _logger.LogWarning(ex, "The DICOM instance file with watermark '{Version}' does not exist.", version);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileFrameAsync(long version, FrameRange range, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(range, nameof(range));

        BlockBlobClient blob = GetInstanceBlockBlobClient(version);

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


    private BlockBlobClient GetInstanceBlockBlobClient(long version)
    {
        string blobName = _nameWithPrefix.GetInstanceFileName(version);

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
}
