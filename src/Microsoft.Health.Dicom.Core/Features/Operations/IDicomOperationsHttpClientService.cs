// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Messages.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Operations
{
    /// <summary>
    /// Represents a client for communicating with DICOM-specific long-running operations via HTTP.
    /// </summary>
    public interface IDicomOperationsHttpClientService
    {
        /// <summary>
        /// Fetches the status of a long-running operation for the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The ID for the operation.</param>
        /// <param name="cancellationToken">
        /// An optional token for cancelling the execution of the
        /// <see cref="GetStatusAsync(string, CancellationToken)"/> operation.
        /// </param>
        /// <returns>
        /// A task representing the <see cref="GetStatusAsync(string, CancellationToken)"/> operation.
        /// The result of the task is the status of the operation with the specified <paramref name="id"/>
        /// </returns>
        Task<OperationStatusResponse> GetStatusAsync(string id, CancellationToken cancellationToken = default);
    }
}
