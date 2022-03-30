// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Operations;

/// <summary>
/// Represents a client for interacting with long-running DICOM operations.
/// </summary>
public interface IDicomOperationsClient
{
    /// <summary>
    /// Fetches the state of a long-running operation for the given <paramref name="operationId"/>.
    /// </summary>
    /// <param name="operationId">The unique ID for a particular DICOM operation.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="GetStateAsync"/> operation. The value of its
    /// <see cref="Task{TResult}.Result"/> property contains the state of the operation
    /// with the specified <paramref name="operationId"/>, if found; otherwise <see langword="null"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<OperationState<DicomOperation>> GetStateAsync(Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins the re-indexing of existing DICOM instances on the tags with the specified <paramref name="tagKeys"/>.
    /// </summary>
    /// <param name="tagKeys">A collection of 1 or more existing query tag keys.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="StartReindexingInstancesAsync(IReadOnlyCollection{int}, CancellationToken)"/>
    /// operation. The value of its <see cref="Task{TResult}.Result"/> property contains the ID of the operation
    /// that is performing the asynchronous addition.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="tagKeys"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="tagKeys"/> is <see langword="null"/>.</exception>
    /// <exception cref="ExtendedQueryTagsAlreadyExistsException">
    /// One or more values in <paramref name="tagKeys"/> has already been indexed.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<Guid> StartReindexingInstancesAsync(IReadOnlyCollection<int> tagKeys, CancellationToken cancellationToken = default);


    Task<Guid> StartExportAsync(ExportInput input, CancellationToken cancellationToken = default);
}
