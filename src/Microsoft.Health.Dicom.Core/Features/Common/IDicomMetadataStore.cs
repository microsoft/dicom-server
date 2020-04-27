// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    /// <summary>
    /// Provides functionalities managing the DICOM instance metadata.
    /// </summary>
    public interface IDicomMetadataStore
    {
        /// <summary>
        /// Asynchronously adds a DICOM instance metadata.
        /// </summary>
        /// <param name="dicomDataset">The DICOM instance.</param>
        /// <param name="version">The version.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous add operation.</returns>
        Task AddInstanceMetadataAsync(
            DicomDataset dicomDataset,
            long version,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a DICOM instance metadata.
        /// </summary>
        /// <param name="dicomInstanceIdentifier">The DICOM instance identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get operation.</returns>
        Task<DicomDataset> GetInstanceMetadataAsync(
            VersionedDicomInstanceIdentifier dicomInstanceIdentifier,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deletes a DICOM instance metadata.
        /// </summary>
        /// <param name="dicomInstanceIdentifier">The DICOM instance identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteInstanceMetadataIfExistsAsync(
            VersionedDicomInstanceIdentifier dicomInstanceIdentifier,
            CancellationToken cancellationToken = default);
    }
}
