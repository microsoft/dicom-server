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
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Model;
using Microsoft.Health.Fhir.SqlServer.Features.Storage;

namespace Microsoft.Health.Fhir.SqlServer.Features.Query
{
    internal class DicomSqlQueryService : IDicomQueryService
    {
        private readonly ILogger<DicomSqlQueryService> _logger;
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

        public DicomSqlQueryService(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
            ILogger<DicomSqlQueryService> logger)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
            _logger = logger;
        }

        public async Task<DicomQueryResult> QueryAsync(
            DicomQueryExpression query,
            CancellationToken cancellationToken)
        {
            var results = new List<QueryResultEntry>(query.EvaluatedLimit);

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapper(true))
            using (SqlCommand sqlCommand = sqlConnectionWrapper.CreateSqlCommand())
            {
                var stringBuilder = new IndentedStringBuilder(new StringBuilder());
                var sqlQueryGenerator = new SqlQueryGenerator(stringBuilder, query, new SqlQueryParameterManager(sqlCommand.Parameters));

                sqlCommand.CommandText = stringBuilder.ToString();
                LogSqlCommand(sqlCommand);

                using (var reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string studyInstanceUID, string seriesInstanceUID, string sOPInstanceUID) = reader.ReadRow(
                           VLatest.Instance.StudyInstanceUID,
                           VLatest.Instance.SeriesInstanceUID,
                           VLatest.Instance.SOPInstanceUID);

                        results.Add(new QueryResultEntry()
                        {
                            StudyInstanceUID = studyInstanceUID,
                            SeriesInstanceUID = seriesInstanceUID,
                            SOPInstanceUID = sOPInstanceUID,
                        });
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
