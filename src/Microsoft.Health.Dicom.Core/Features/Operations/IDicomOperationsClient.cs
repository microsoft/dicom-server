// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Messages.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Operations
{
    /// <summary>
    /// Represents a client for interacting with long-running DICOM operations.
    /// </summary>
    public interface IDicomOperationsClient
    {
        /// <summary>
        /// Fetches the status of a long-running operation for the given <paramref name="operationId"/>.
        /// </summary>
        /// <param name="operationId">The unique ID for a particular DICOM operation.</param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the <see cref="GetStatusAsync(string, CancellationToken)"/> operation.
        /// The value of its <see cref="Task{TResult}.Result"/> property contains the status of the operation
        /// with the specified <paramref name="operationId"/>, if found; otherwise <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="operationId"/> consists of white space characters.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="operationId"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
        Task<OperationStatusResponse> GetStatusAsync(string operationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins the addition of one or more extended query tags such that they can be used in QIDO.
        /// </summary>
        /// <param name="tags">A collection of 1 or more query tags.</param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the <see cref="StartExtendedQueryTagAdditionAsync(ICollection{AddExtendedQueryTagEntry}, CancellationToken)"/>
        /// operation. The value of its <see cref="Task{TResult}.Result"/> property contains the ID of the operation
        /// that is performing the asynchronous addition.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="tags"/> is empty..</exception>
        /// <exception cref="ArgumentNullException"><paramref name="tags"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
        Task<string> StartExtendedQueryTagAdditionAsync(ICollection<AddExtendedQueryTagEntry> tags, CancellationToken cancellationToken = default);
    }
}
