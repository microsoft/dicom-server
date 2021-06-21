// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.SqlServer.Features.Common
{
    /// <summary>
    /// Represents a factory for creating connections for a database, like SQL.
    /// </summary>
    // TODO: Move to shared componenets
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// Asynchronously retrieves a connection.
        /// </summary>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the asychronous operation. The value of its <see cref="Task{TResult}.Result"/>
        /// property contains the <see cref="DbConnection"/>.
        /// </returns>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
        Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
    }
}
