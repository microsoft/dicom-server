// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    /// <summary>
    /// Provides functionalities managing the DICOM files.
    /// </summary>
    public interface IDicomFileStore
    {
        /// <summary>
        /// Asynchronously adds a file to the file store.
        /// </summary>
        /// <param name="dicomInstanceIdentifier">The DICOM identifier.</param>
        /// <param name="stream">The DICOM instance stream.</param>
        /// <param name="overwriteIfExists">A flag indicating to overwrite the existing file or not.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous add operation.</returns>
        Task<Uri> AddFileAsync(VersionedDicomInstanceIdentifier dicomInstanceIdentifier, Stream stream, bool overwriteIfExists = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a file from the file store.
        /// </summary>
        /// <param name="dicomInstanceIdentifier">The DICOM identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get operation./returns>
        Task<Stream> GetFileAsync(VersionedDicomInstanceIdentifier dicomInstanceIdentifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deletes a file from the file store if the file exists.
        /// </summary>
        /// <param name="dicomInstanceIdentifier">The DICOM identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteFileIfExistsAsync(VersionedDicomInstanceIdentifier dicomInstanceIdentifier, CancellationToken cancellationToken = default);
    }
}
