// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Store.Upload
{
    /// <summary>
    /// Provides functionality to read uploaded DICOM instance from stream such as HTTP request body.
    /// </summary>
    public interface IUploadedDicomInstanceReader
    {
        /// <summary>
        /// Gets a flag indicating whether this reader can read the HTTP request body with <paramref name="contentType"/>.
        /// </summary>
        /// <param name="contentType">The content type.</param>
        /// <returns><c>true</c> if the reader can read the content; otherwise, <c>false</c>.</returns>
        bool CanRead(string contentType);

        /// <summary>
        /// Reads the uploaded DICOM instances.
        /// </summary>
        /// <param name="contentType">The content type.</param>
        /// <param name="stream">The stream to read the DICOM instances from.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of <see cref="IUploadedDicomInstance"/>.</returns>
        Task<IReadOnlyCollection<IUploadedDicomInstance>> ReadAsync(string contentType, Stream stream, CancellationToken cancellationToken = default);
    }
}
