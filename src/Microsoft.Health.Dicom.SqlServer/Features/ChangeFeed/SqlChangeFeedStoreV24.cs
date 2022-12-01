// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed;

internal class SqlChangeFeedStoreV24 : SqlChangeFeedStoreV6
{
    public override SchemaVersion Version => SchemaVersion.V24;

    public SqlChangeFeedStoreV24(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
       : base(sqlConnectionWrapperFactory)
    {
    }

    public override async Task<IReadOnlyCollection<ChangeFeedEntry>> GetDeletedChangeFeedByWatermarkOrTimeStampAsync(
        int batchCount,
        DateTime? timeStamp,
        WatermarkRange? watermarkRange,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ChangeFeedEntry>();

        using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

        var startWatermark = watermarkRange.HasValue ? watermarkRange.Value.Start : default;
        var endWatermark = watermarkRange.HasValue ? watermarkRange.Value.End : default;

        VLatest.GetDeletedChangeFeedByWatermarkOrTimeStamp.PopulateCommand(sqlCommandWrapper, batchCount, timeStamp, startWatermark, endWatermark);

        using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            (long rSeq, DateTimeOffset rTimestamp, int rAction, string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid, long oWatermark, long? cWatermark) = reader.ReadRow(
                VLatest.ChangeFeed.Sequence,
                VLatest.ChangeFeed.Timestamp,
                VLatest.ChangeFeed.Action,
                VLatest.ChangeFeed.StudyInstanceUid,
                VLatest.ChangeFeed.SeriesInstanceUid,
                VLatest.ChangeFeed.SopInstanceUid,
                VLatest.ChangeFeed.OriginalWatermark,
                VLatest.ChangeFeed.CurrentWatermark);

            results.Add(new ChangeFeedEntry(
                    rSeq,
                    rTimestamp,
                    (ChangeFeedAction)rAction,
                    rStudyInstanceUid,
                    rSeriesInstanceUid,
                    rSopInstanceUid,
                    oWatermark,
                    cWatermark,
                    ConvertWatermarkToCurrentState(oWatermark, cWatermark)));
        }

        return results;
    }

    public override async Task<long> GetMaxDeletedChangeFeedWatermarkAsync(DateTime timeStamp, CancellationToken cancellationToken = default)
    {
        using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

        VLatest.GetMaxDeletedChangeFeedWatermark.PopulateCommand(sqlCommandWrapper, timeStamp);
        return (long)(await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));
    }
}
