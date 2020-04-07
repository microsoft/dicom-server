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
    public interface IDicomInstanceEntry : IAsyncDisposable
    {
        /// <summary>
        /// Gets the <see cref="DicomDataset"/> of the DICOM instance.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An instance of <see cref="DicomDataset"/>.</returns>
        /// <exception cref="InvalidDicomInstanceException">Thrown when the uploaded DICOM instance is invalid.</exception>
        ValueTask<DicomDataset> GetDicomDatasetAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the <see cref="Stream"/> of the DICOM instance.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An instance of <see cref="Stream"/>.</returns>
        ValueTask<Stream> GetStreamAsync(CancellationToken cancellationToken);
    }
}
