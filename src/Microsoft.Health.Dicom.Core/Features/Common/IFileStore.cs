// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    /// <summary>
    /// Provides functionality to manage the DICOM files.
    /// </summary>
    public interface IFileStore
    {
        /// <summary>
        /// Asynchronously stores a file to the file store.
        /// </summary>
        /// <param name="versionedInstanceIdentifier">The DICOM identifier.</param>
        /// <param name="stream">The DICOM instance stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous add operation.</returns>
        Task<Uri> StoreFileAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a file from the file store.
        /// </summary>
        /// <param name="versionedInstanceIdentifier">The DICOM identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get operation.</returns>
        Task<Stream> GetFileAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deletes a file from the file store if the file exists.
        /// </summary>
        /// <param name="versionedInstanceIdentifier">The DICOM identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteFileIfExistsAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously get a specific range of bytes from the blob
        /// </summary>
        /// <param name="versionedInstanceIdentifier">The DICOM identifier.</param>
        /// <param name="range">Byte range in Httprange format with offset and length</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Stream representing the bytes requested</returns>
        Task<Stream> GetFileFrameAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, FrameRange range, CancellationToken cancellationToken = default);
    }
}
