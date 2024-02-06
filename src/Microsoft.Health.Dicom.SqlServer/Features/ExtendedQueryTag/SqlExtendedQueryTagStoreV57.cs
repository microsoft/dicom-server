// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

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

    public override async Task UpdateExtendedQueryTagStatusToDelete(int tagKey, CancellationToken cancellationToken = default)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.UpdateExtendedQueryTagStatusToDelete.PopulateCommand(sqlCommandWrapper, tagKey);

            try
            {
                await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw ex.Number switch
                {
                    SqlErrorCodes.NotFound =>
                        throw new ExtendedQueryTagNotFoundException(
                            string.Format(CultureInfo.InvariantCulture, DicomSqlServerResource.ExtendedQueryTagNotFound, tagKey)),
                    SqlErrorCodes.PreconditionFailed =>
                        throw new ExtendedQueryTagBusyException(
                            string.Format(CultureInfo.InvariantCulture, DicomSqlServerResource.ExtendedQueryTagIsBusy, tagKey)),
                    _ =>
                        throw new DataStoreException(ex)
                };
            }
        }
    }

    public override async Task<IReadOnlyList<WatermarkRange>> GetExtendedQueryTagBatches(
        int batchSize,
        int batchCount,
        string vr,
        int tagKey,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsGt(batchSize, 0, nameof(batchSize));
        EnsureArg.IsGt(batchCount, 0, nameof(batchCount));

        using SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

        VLatest.GetExtendedQueryTagBatches.PopulateCommand(sqlCommandWrapper, batchSize, batchCount, (byte)ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[vr], tagKey);

        try
        {
            var batches = new List<WatermarkRange>();
            using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                batches.Add(new WatermarkRange(reader.GetInt64(0), reader.GetInt64(1)));
            }

            return batches;
        }
        catch (SqlException ex)
        {
            throw new DataStoreException(ex);
        }
    }

    public override async Task DeleteExtendedQueryTagDataByWatermarkRangeAsync(
        long startWatermark,
        long endWatermark,
        string vr,
        int tagKey,
        CancellationToken cancellationToken = default)
    {
        using SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

        VLatest.DeleteExtendedQueryTagDataByWatermarkRange.PopulateCommand(sqlCommandWrapper, startWatermark, endWatermark, (byte)ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[vr], tagKey);

        try
        {
            await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex)
        {
            throw new DataStoreException(ex);
        }
    }
}
