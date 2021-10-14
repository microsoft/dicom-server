// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    internal class SqlQueryStoreV4 : ISqlQueryStore
    {
        public virtual SchemaVersion Version => SchemaVersion.V4;

        protected SqlConnectionWrapperFactory SqlConnectionWrapperFactory;

        private readonly ILogger<ISqlQueryStore> _logger;
        private const string DefaultRedactedValue = "***";

        public SqlQueryStoreV4(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
            ILogger<ISqlQueryStore> logger)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            SqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
            _logger = logger;
        }

        public virtual async Task<QueryResult> QueryAsync(
            QueryExpression query,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(query, nameof(query));

            var results = new List<VersionedInstanceIdentifier>(query.EvaluatedLimit);

            using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand();

            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var sqlQueryGenerator = new SqlQueryGenerator(stringBuilder, query, new SqlQueryParameterManager(sqlCommandWrapper.Parameters), Version);

            sqlCommandWrapper.CommandText = stringBuilder.ToString();
            LogSqlCommand(sqlCommandWrapper);

            using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                (string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long watermark) = reader.ReadRow(
                   V4.Instance.StudyInstanceUid,
                   V4.Instance.SeriesInstanceUid,
                   V4.Instance.SopInstanceUid,
                   V4.Instance.Watermark);

                results.Add(new VersionedInstanceIdentifier(
                        studyInstanceUid,
                        seriesInstanceUid,
                        sopInstanceUid,
                        watermark));
            }

            return new QueryResult(results);
        }

        protected void LogSqlCommand(SqlCommandWrapper sqlCommandWrapper)
        {
            var sb = new StringBuilder();
            foreach (SqlParameter p in sqlCommandWrapper.Parameters)
            {
                sb.Append("DECLARE ")
                    .Append(p)
                    .Append(' ')
                    .Append(p.SqlDbType)
                    .Append(p.Value is string ? $"({p.Size})" : p.Value is decimal ? $"({p.Precision},{p.Scale})" : null)
                    .Append(" = ")
                    .Append(DefaultRedactedValue)
                    .Append(';')
                    .AppendLine();
            }

            sb.AppendLine();

            sb.AppendLine(sqlCommandWrapper.CommandText);
            _logger.LogInformation(sb.ToString());
        }
    }
}
