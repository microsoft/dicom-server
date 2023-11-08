// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Core.Features.Common;

/// <summary>
/// Provides functionality to manage the DICOM files.
/// </summary>
public interface IFileStore
{
    /// <summary>
    /// Asynchronously stores a file to the file store.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partitionName">Name of the partition</param>
    /// <param name="stream">The DICOM instance stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    Task<FileProperties> StoreFileAsync(long version, string partitionName, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a file from the file store.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partition">Partition to use when operating on file</param>
    /// <param name="fileProperties">blob file Properties</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous get operation.</returns>
    Task<Stream> GetFileAsync(long version, Partition partition, FileProperties fileProperties, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes a file from the file store if the file exists.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partition">Partition of the instance to be deleted</param>
    /// <param name="fileProperties">File properties of instance to be deleted</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteFileIfExistsAsync(long version, Partition partition, FileProperties fileProperties, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously get file properties
    /// </summary>
    /// <param name="version">The DICOM instance version when file properties not known.</param>
    /// <param name="partition">Partition of the instance to get file properties on when file properties not known</param>
    /// <param name="fileProperties">When file properties known, will use to get content length and match on etag</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous get properties operation.</returns>
    Task<FileProperties> GetFilePropertiesAsync(long version, Partition partition, FileProperties fileProperties, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously get a specific range of bytes from the blob
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partition">Partition within which the blob exists</param>
    /// <param name="range">Byte range in Httprange format with offset and length</param>
    /// <param name="fileProperties">File properties of blob to use to get it</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Stream representing the bytes requested</returns>
    Task<Stream> GetFileFrameAsync(
        long version,
        Partition partition,
        FrameRange range,
        FileProperties fileProperties,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a streaming file from the file store.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partition">Partition within which the blob exists</param>
    /// <param name="fileProperties">File properties of blob to use to get it</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous get operation.</returns>
    Task<Stream> GetStreamingFileAsync(long version, Partition partition, FileProperties fileProperties, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously stores a file to the file store in blocks.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partition">Partition to use when storing file</param>
    /// <param name="stream">The DICOM instance stream.</param>
    /// <param name="blockLengths">Dictionary of blockId and block lengths</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous Store operation.</returns>
    Task<FileProperties> StoreFileInBlocksAsync(long version, Partition partition, Stream stream, IDictionary<string, long> blockLengths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a file content from the file store. The file content will be in memory. Use only for small files
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partition">Partition to use when operating on file</param>
    /// <param name="fileProperties">blob file Properties</param>
    /// <param name="range">Byte range in Httprange format with offset and length</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<BinaryData> GetFileContentInRangeAsync(long version, Partition partition, FileProperties fileProperties, FrameRange range, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously updates a block in a blob.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partition">Partition to use when storing file</param>
    /// <param name="fileProperties">blob file Properties</param>
    /// <param name="blockId">BlockId to be updated.</param>
    /// <param name="stream">The DICOM instance stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous update block operation.</returns>
    Task<FileProperties> UpdateFileBlockAsync(long version, Partition partition, FileProperties fileProperties, string blockId, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a commited block list.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partition">Partition to use when storing file</param>
    /// <param name="fileProperties">Partition to use when operating on file</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Key value pair of blockId and blockLength.</returns>
    Task<KeyValuePair<string, long>> GetFirstBlockPropertyAsync(long version, Partition partition, FileProperties fileProperties, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously copies file from the same container
    /// </summary>
    /// <param name="originalVersion">The DICOM instance original version.</param>
    /// <param name="newVersion">The DICOM instance new version.</param>
    /// <param name="partition">Partition to use when operating on file</param>
    /// <param name="fileProperties">blob file Properties</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task CopyFileAsync(long originalVersion, long newVersion, Partition partition, FileProperties fileProperties, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously change blob access tier to cold tier.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partition">Partition to use when operating on file</param>
    /// <param name="fileProperties">Blob file Properties</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task SetBlobToColdAccessTierAsync(long version, Partition partition, FileProperties fileProperties, CancellationToken cancellationToken = default);
}
