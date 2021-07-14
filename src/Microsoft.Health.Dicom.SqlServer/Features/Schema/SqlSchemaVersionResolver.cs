// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.SqlServer.Features.Schema.Manager;

namespace Microsoft.Health.Dicom.SqlServer.Features.Schema
{
    /// <summary>
    /// Represents an <see cref="ISchemaVersionResolver"/> that determines the version present in a SQL server.
    /// </summary>
    public class SqlSchemaVersionResolver : ISchemaVersionResolver
    {
        private readonly IReadOnlySchemaManagerDataStore _schemaManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlSchemaVersionResolver"/> class.
        /// </summary>
        /// <param name="schemaManager">A read-only manager for the application database version.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="schemaManager"/> is <see langword="null"/>.
        /// </exception>
        public SqlSchemaVersionResolver(IReadOnlySchemaManagerDataStore schemaManager)
        {
            _schemaManager = EnsureArg.IsNotNull(schemaManager, nameof(schemaManager));
        }

        /// <summary>
        /// Asynchronously fetches the current version from the SQL database.
        /// </summary>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the asychronous operation. The value of its <see cref="Task{TResult}.Result"/>
        /// property contains the current version in the SQL database.
        /// </returns>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
        public async Task<SchemaVersion> GetCurrentVersionAsync(CancellationToken cancellationToken = default)
            => (SchemaVersion)await _schemaManager.GetCurrentSchemaVersionAsync(cancellationToken);
    }
}
