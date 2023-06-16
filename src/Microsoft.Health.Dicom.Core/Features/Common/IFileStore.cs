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
    /// <param name="partitionName">Name of the partition</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous get operation.</returns>
    Task<Stream> GetFileAsync(long version, string partitionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes a file from the file store if the file exists.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partitionName">Name of the partition</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteFileIfExistsAsync(long version, string partitionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously get file properties
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partitionName">Name of the partition</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous get properties operation.</returns>
    Task<FileProperties> GetFilePropertiesAsync(long version, string partitionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously get a specific range of bytes from the blob
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partitionName">Name of the partition</param>
    /// <param name="range">Byte range in Httprange format with offset and length</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Stream representing the bytes requested</returns>
    Task<Stream> GetFileFrameAsync(
        long version,
        string partitionName,
        FrameRange range,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a streaming file from the file store.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="partitionName">Name of the partition</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous get operation.</returns>
    Task<Stream> GetStreamingFileAsync(long version, string partitionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously stores a file to the file store in blocks.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="stream">The DICOM instance stream.</param>
    /// <param name="blockLengths">Dictionary of blockId and block lengths</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous Store operation.</returns>
    Task<Uri> StoreFileInBlocksAsync(long version, Stream stream, IDictionary<string, long> blockLengths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a file content from the file store. The file content will be in memory. Use only for small files
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="range">Byte range in Httprange format with offset and length</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<BinaryData> GetFileContentInRangeAsync(long version, FrameRange range, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously updates a block in a blob.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="blockId">BlockId to be updated.</param>
    /// <param name="stream">The DICOM instance stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous update block operation.</returns>
    Task UpdateFileBlockAsync(long version, string blockId, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a commited block list.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Key value pair of blockId and blockLength.</returns>
    Task<KeyValuePair<string, long>> GetFirstBlockPropertyAsync(long version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously copies file from the same container
    /// </summary>
    /// <param name="originalVersion">The DICOM instance original version.</param>
    /// <param name="newVersion">The DICOM instance new version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task CopyFileAsync(long originalVersion, long newVersion, CancellationToken cancellationToken = default);
}
