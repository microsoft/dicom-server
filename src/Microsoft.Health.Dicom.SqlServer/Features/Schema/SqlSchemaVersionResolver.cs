// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer;

namespace Microsoft.Health.Dicom.SqlServer.Features.Schema
{
    /// <summary>
    /// Represents an <see cref="ISchemaVersionResolver"/> that determines the version present in a SQL server.
    /// </summary>
    public class SqlSchemaVersionResolver : ISchemaVersionResolver
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly ILogger<SqlSchemaVersionResolver> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlSchemaVersionResolver"/> class.
        /// </summary>
        /// <param name="sqlConnectionFactory">A factory for creating SQL connections.</param>
        /// <param name="logger">A typed logger for diagnostic information.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="sqlConnectionFactory"/> or <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        public SqlSchemaVersionResolver(ISqlConnectionFactory sqlConnectionFactory, ILogger<SqlSchemaVersionResolver> logger)
        {
            EnsureArg.IsNotNull(sqlConnectionFactory, nameof(sqlConnectionFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlConnectionFactory = sqlConnectionFactory;
            _logger = logger;
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
            const string tableName = "dbo.SchemaVersion";

            using SqlConnection connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using SqlCommand selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT MAX(Version) FROM " + tableName + " WHERE Status = 'complete' OR Status = 'completed'";

            try
            {
                var version = await selectCommand.ExecuteScalarAsync(cancellationToken) as int?;
                return (SchemaVersion)version.GetValueOrDefault();
            }
            catch (SqlException e) when (e.Message is "Invalid object name 'dbo.SchemaVersion'.")
            {
                _logger.LogWarning("The table {TableName} does not exists. It must be new database", tableName);
                return SchemaVersion.Unknown;
            }
        }
    }
}
