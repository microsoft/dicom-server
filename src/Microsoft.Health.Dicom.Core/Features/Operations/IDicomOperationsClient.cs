// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Core.Models.Update;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Operations;

/// <summary>
/// Represents a client for interacting with long-running DICOM operations.
/// </summary>
public interface IDicomOperationsClient
{
    /// <summary>
    /// Asynchronously retrieves the state of a long-running operation for the given <paramref name="operationId"/>.
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
    Task<IOperationState<DicomOperation>> GetStateAsync(Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously searches for long-running operations based on the given <paramref name="query"/>.
    /// </summary>
    /// <param name="query">A set of operation search criteria.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>An asynchronous enumeration of results based on the <paramref name="query"/>.</returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    IAsyncEnumerable<OperationReference> FindOperationsAsync(OperationQueryCondition<DicomOperation> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the state of a long-running operation for the given <paramref name="operationId"/> with checkpoint information.
    /// </summary>
    /// <param name="operationId">The unique ID for a particular DICOM operation.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="GetLastCheckpointAsync"/> operation. The value of its
    /// <see cref="Task{TResult}.Result"/> property contains the state of the operation
    /// with the specified <paramref name="operationId"/>, if found; otherwise <see langword="null"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<OperationCheckpointState<DicomOperation>> GetLastCheckpointAsync(Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously begins the re-indexing of existing DICOM instances on the tags with the specified <paramref name="tagKeys"/>.
    /// </summary>
    /// <param name="operationId">The desired ID for the long-running re-index operation.</param>
    /// <param name="tagKeys">A collection of 1 or more existing query tag keys.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="StartReindexingInstancesAsync"/>
    /// operation. The value of its <see cref="Task{TResult}.Result"/> property contains a reference
    /// to the newly started operation.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="tagKeys"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="tagKeys"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<OperationReference> StartReindexingInstancesAsync(Guid operationId, IReadOnlyCollection<int> tagKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously begins the export of files as detailed in the given <paramref name="specification"/>.
    /// </summary>
    /// <param name="operationId">The desired ID for the long-running export operation.</param>
    /// <param name="specification">The specification that details the source and destination for the export.</param>
    /// <param name="partition">The partition containing the data to export.</param>
    /// <param name="errorHref">The <see cref="Uri"/> for the export error log.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="StartReindexingInstancesAsync"/>
    /// operation. The value of its <see cref="Task{TResult}.Result"/> property contains a reference
    /// to the newly started operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="specification"/>, <paramref name="errorHref"/>, or <paramref name="partition"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<OperationReference> StartExportAsync(Guid operationId, ExportSpecification specification, Uri errorHref, Partition partition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously begins the update operation in the given <paramref name="updateSpecification"/>.
    /// </summary>
    /// <param name="operationId">The desired ID for the long-running update operation.</param>
    /// <param name="updateSpecification">The specification that details the update changed dataset for updating studies</param>
    /// <param name="partition">The partition containing the data to update.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>
    /// A task representing the <see cref="StartUpdateOperationAsync"/>
    /// operation. The value of its <see cref="Task{TResult}.Result"/> property contains a reference
    /// to the newly started operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="updateSpecification"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<OperationReference> StartUpdateOperationAsync(Guid operationId, UpdateSpecification updateSpecification, Partition partition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously begins the clean up of instance data.
    /// </summary>
    /// <param name="operationId">The desired ID for the cleanup operation.</param>
    /// <param name="startFilterTimeStamp">Start timestamp to filter instances.</param>
    /// <param name="endFilterTimeStamp">End timestamp to filter instances.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>
    /// A task representing the <see cref="StartInstanceDataCleanupOperationAsync"/> operation.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task StartInstanceDataCleanupOperationAsync(Guid operationId, DateTimeOffset startFilterTimeStamp, DateTimeOffset endFilterTimeStamp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously begins the backfill of content length for DICOM instances.
    /// </summary>
    /// <param name="operationId">The desired ID for the operation.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>
    /// A task representing the <see cref="StartContentLengthBackFillOperationAsync"/> operation.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task StartContentLengthBackFillOperationAsync(Guid operationId, CancellationToken cancellationToken = default);
}
