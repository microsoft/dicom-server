// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Core.Features.Update;

public interface IUpdateInstanceService
{
    /// <summary>
    /// Asynchronously update instance blobs
    /// </summary>
    /// <param name="instance">Instance to update</param>
    /// <param name="datasetToUpdate">Dataset to update</param>
    /// <param name="partition">Partition to update data within</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous UpdateInstanceBlobAsync operation</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="datasetToUpdate"/> is <see langword="null"/>.
    /// </exception>
    public Task<FileProperties> UpdateInstanceBlobAsync(InstanceMetadata instance, DicomDataset datasetToUpdate, Partition partition, CancellationToken cancellationToken = default);
    /// <summary>
    /// Asynchronously deletes old blob
    /// </summary>
    /// <param name="fileIdentifier">Unique file identifier, watermark</param>
    /// <param name="partition">Partition</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous DeleteInstanceBlobAsync operation</returns>
    public Task DeleteInstanceBlobAsync(long fileIdentifier, Partition partition, CancellationToken cancellationToken = default);
}
