// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Store.Entries
{
    /// <summary>
    /// Represents a DICOM instance entry that's been read from the HTTP request body.
    /// </summary>
    public interface IInstanceEntry : IAsyncDisposable
    {
        /// <summary>
        /// Gets the <see cref="DicomDataset"/> of the DICOM instance entry.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An instance of <see cref="DicomDataset"/>.</returns>
        /// <exception cref="InvalidDicomInstanceException">Thrown when the DICOM instance entry is invalid.</exception>
        ValueTask<DicomDataset> GetDicomDatasetAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the <see cref="Stream"/> of the DICOM instance entry.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An instance of <see cref="Stream"/>.</returns>
        ValueTask<Stream> GetStreamAsync(CancellationToken cancellationToken);
    }
}
