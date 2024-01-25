// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag.Errors;

internal class SqlExtendedQueryTagErrorStoreV57 : SqlExtendedQueryTagErrorStoreV36
{
    public SqlExtendedQueryTagErrorStoreV57(SqlConnectionWrapperFactory sqlConnectionWrapperFactory, ILogger<SqlExtendedQueryTagErrorStoreV57> logger)
        : base(sqlConnectionWrapperFactory, logger)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V57;

    public override async Task<int> DeleteExtendedQueryTagErrorBatch(int tagKey, int batchSize, CancellationToken cancellationToken = default)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.DeleteExtendedQueryTagErrorBatch.PopulateCommand(sqlCommandWrapper, tagKey, batchSize);

            using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken);
            return reader.RecordsAffected;
        }
    }
}
