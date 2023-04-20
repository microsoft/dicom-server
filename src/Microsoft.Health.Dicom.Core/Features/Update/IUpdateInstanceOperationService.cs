// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Messages.Update;
using Microsoft.Health.Dicom.Core.Models.Update;

namespace Microsoft.Health.Dicom.Core.Features.Update;

/// <summary>
/// Provides functionality to queue the operation for updating the study attributes
/// for a list of studyInstanceUids.
/// </summary>
public interface IUpdateInstanceOperationService
{
    /// <summary>
    /// Queues the operation for updating the <see cref="UpdateSpecification"/>.
    /// </summary>
    /// <param name="updateSpecification">Update spec that has the studyInstanceUids and DicomDataset</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous process operation.</returns>
    public Task<UpdateInstanceResponse> QueueUpdateOperationAsync(
        UpdateSpecification updateSpecification,
        CancellationToken cancellationToken = default);
}
