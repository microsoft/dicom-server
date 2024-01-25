// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
internal class SqlExtendedQueryTagStoreV57 : SqlExtendedQueryTagStoreV36
{
    /// <summary>
    /// TODO: Error handling for new sprocs
    /// </summary>
    /// <param name="sqlConnectionWrapperFactory"></param>
    /// <param name="logger"></param>
    public SqlExtendedQueryTagStoreV57(SqlConnectionWrapperFactory sqlConnectionWrapperFactory, ILogger<SqlExtendedQueryTagStoreV57> logger)
        : base(sqlConnectionWrapperFactory, logger)
    {
    }

    // just deletes the tag itself
    public override async Task DeleteExtendedQueryTagEntryAsync(int tagKey, CancellationToken cancellationToken = default)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.DeleteExtendedQueryTagEntry.PopulateCommand(sqlCommandWrapper, tagKey);

            await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public override async Task<int> DeleteExtendedQueryTagIndexBatchAsync(int tagKey, string vr, int batchSize, CancellationToken cancellationToken = default)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.DeleteExtendedQueryTagIndexBatch.PopulateCommand(sqlCommandWrapper, tagKey, (byte)ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[vr], batchSize);

            using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken);
            return reader.RecordsAffected;
        }
    }

    public override async Task UpdateExtendedQueryTagStatusAsync(int tagKey, ExtendedQueryTagStatus status, CancellationToken cancellationToken = default)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.UpdateExtendedQueryTagStatus.PopulateCommand(sqlCommandWrapper, tagKey, (byte)status);

            await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
