// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    internal class DicomSqlQueryStore : IDicomQueryStore
    {
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly ILogger<DicomSqlQueryStore> _logger;

        public DicomSqlQueryStore(
            SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration,
            ILogger<DicomSqlQueryStore> logger)
        {
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
            _logger = logger;
        }

        public async Task<DicomQueryResult> QueryAsync(
            DicomQueryExpression query,
            CancellationToken cancellationToken)
        {
            var results = new List<DicomInstanceIdentifier>(query.EvaluatedLimit);

            using (var sqlConnection = new SqlConnection(_sqlServerDataStoreConfiguration.ConnectionString))
            using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
            {
                await sqlCommand.Connection.OpenAsync(cancellationToken);
                var stringBuilder = new IndentedStringBuilder(new StringBuilder());
                var sqlQueryGenerator = new SqlQueryGenerator(stringBuilder, query, new SqlQueryParameterManager(sqlCommand.Parameters));

                sqlCommand.CommandText = stringBuilder.ToString();
                LogSqlCommand(sqlCommand);

                using (var reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long watermark) = reader.ReadRow(
                           VLatest.Instance.StudyInstanceUid,
                           VLatest.Instance.SeriesInstanceUid,
                           VLatest.Instance.SopInstanceUid,
                           VLatest.Instance.Watermark);

                        results.Add(new VersionedDicomInstanceIdentifier(
                                studyInstanceUid,
                                seriesInstanceUid,
                                sopInstanceUid,
                                watermark));
                    }
                }
            }

            return new DicomQueryResult(results);
        }

        private void LogSqlCommand(SqlCommand sqlCommand)
        {
            var sb = new StringBuilder();
            foreach (SqlParameter p in sqlCommand.Parameters)
            {
                sb.Append("DECLARE ")
                    .Append(p)
                    .Append(" ")
                    .Append(p.SqlDbType)
                    .Append(p.Value is string ? $"({p.Size})" : p.Value is decimal ? $"({p.Precision},{p.Scale})" : null)
                    .Append(" = ")
                    .Append(p.SqlDbType == SqlDbType.NChar || p.SqlDbType == SqlDbType.NText || p.SqlDbType == SqlDbType.NVarChar ? "N" : null)
                    .Append(p.Value is string || p.Value is DateTime ? $"'{p.Value}'" : p.Value.ToString())
                    .AppendLine(";");
            }

            sb.AppendLine();

            sb.AppendLine(sqlCommand.CommandText);
            _logger.LogInformation(sb.ToString());
        }
    }
}
