// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.SqlServer.Features.Schema
{
    /// <summary>
    /// An abstraction for retrieving the version of the underlying versioned store.
    /// </summary>
    public interface ISchemaVersionResolver
    {
        /// <summary>
        /// Asynchronously fetches the current version from the store.
        /// </summary>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the asychronous operation. The value of its <see cref="Task{TResult}.Result"/>
        /// property contains the current version.
        /// </returns>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
        Task<SchemaVersion> GetCurrentVersionAsync(CancellationToken cancellationToken = default);
    }
}
