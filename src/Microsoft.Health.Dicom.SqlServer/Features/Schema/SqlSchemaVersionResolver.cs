// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.SqlServer.Features.Common;

namespace Microsoft.Health.Dicom.SqlServer.Features.Schema
{
    /// <summary>
    /// Represents an <see cref="ISchemaVersionResolver"/> that determines the version present in a SQL server.
    /// </summary>
    public class SqlSchemaVersionResolver : ISchemaVersionResolver
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        internal const string VersionStoredProcedure = "dbo.SelectCurrentSchemaVersion";

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlSchemaVersionResolver"/> class.
        /// </summary>
        /// <param name="dbConnectionFactory">A factory for creating SQL connections.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dbConnectionFactory"/> is <see langword="null"/>.
        /// </exception>
        public SqlSchemaVersionResolver(IDbConnectionFactory dbConnectionFactory)
        {
            EnsureArg.IsNotNull(dbConnectionFactory, nameof(dbConnectionFactory));
            _dbConnectionFactory = dbConnectionFactory;
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
        {
            using DbConnection connection = await _dbConnectionFactory.GetConnectionAsync(cancellationToken);
            await connection.OpenAsync(cancellationToken);

            using DbCommand selectCommand = connection.CreateCommand();
            selectCommand.CommandType = CommandType.StoredProcedure;
            selectCommand.CommandText = VersionStoredProcedure;

            // TODO: If we cannot find the SP, should we retry because it's a new DB?
            object current = await selectCommand.ExecuteScalarAsync(cancellationToken);
            return (current == null || Convert.IsDBNull(current)) ? SchemaVersion.Unknown : (SchemaVersion)current;
        }
    }
}
