// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Update;
public interface IUpdateInstanceService
{
    /// <summary>
    /// Asynchronously update instance blobs
    /// </summary>
    /// <param name="instanceMetadata">Instance metadata</param>
    /// <param name="datasetToUpdate">Dataset to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous UpdateInstanceBlobAsync operation</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="instanceMetadata"/> or <paramref name="datasetToUpdate"/> is <see langword="null"/>.
    /// </exception>
    public Task UpdateInstanceBlobAsync(InstanceMetadata instanceMetadata, DicomDataset datasetToUpdate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes old blob
    /// </summary>
    /// <param name="fileIdentifier">Unique file identifier, watermark</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous DeleteInstanceBlobAsync operation</returns>
    public Task DeleteInstanceBlobAsync(long fileIdentifier, CancellationToken cancellationToken = default);
}
