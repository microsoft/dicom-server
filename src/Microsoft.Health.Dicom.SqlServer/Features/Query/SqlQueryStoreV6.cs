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
    internal class SqlQueryStoreV6 : SqlQueryStoreV5
    {
        public override SchemaVersion Version => SchemaVersion.V6;

        public SqlQueryStoreV6(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
            ILogger<ISqlQueryStore> logger)
            : base(sqlConnectionWrapperFactory, logger)
        {
        }

        public override async Task<QueryResult> QueryAsync(
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
                   VLatest.Instance.StudyInstanceUid,
                   VLatest.Instance.SeriesInstanceUid,
                   VLatest.Instance.SopInstanceUid,
                   VLatest.Instance.Watermark);

                results.Add(new VersionedInstanceIdentifier(
                        studyInstanceUid,
                        seriesInstanceUid,
                        sopInstanceUid,
                        watermark));
            }

            return new QueryResult(results);
        }
    }
}
