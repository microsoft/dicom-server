// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Common;

/// <summary>
/// Provides functionalities managing the DICOM instance metadata.
/// </summary>
public interface IMetadataStore
{
    /// <summary>
    /// Asynchronously stores a DICOM instance metadata.
    /// </summary>
    /// <param name="dicomDataset">The DICOM instance.</param>
    /// <param name="version">The version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    Task StoreInstanceMetadataAsync(
        DicomDataset dicomDataset,
        long version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a DICOM instance metadata.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous get operation.</returns>
    Task<DicomDataset> GetInstanceMetadataAsync(
        long version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes a DICOM instance metadata.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteInstanceMetadataIfExistsAsync(
        long version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Async store Frames range metadata
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="framesRange">Dictionary of frame id and byte range</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the async add of frame metadata</returns>
    Task StoreInstanceFramesRangeAsync(
            long version,
            IReadOnlyDictionary<int, FrameRange> framesRange,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Async get Frames range metadata
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Dictionary of frame id and byte range</returns>
    Task<IReadOnlyDictionary<int, FrameRange>> GetInstanceFramesRangeAsync(
        long version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes a DICOM instance frameRange metadata.
    /// </summary>
    /// <param name="version">The DICOM instance version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteInstanceFramesRangeAsync(
        long version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the frame range exist for the given version
    /// </summary>
    /// <param name="version"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> DoesFrameRangeExistAsync(long version, CancellationToken cancellationToken = default);
}
