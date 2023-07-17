// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed;

internal class SqlChangeFeedStoreV36 : SqlChangeFeedStoreV4
{
    public override SchemaVersion Version => SchemaVersion.V36;

    public SqlChangeFeedStoreV36(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
       : base(sqlConnectionWrapperFactory)
    {
    }

    public override async Task<IReadOnlyList<ChangeFeedEntry>> GetChangeFeedAsync(TimeRange range, long offset, int limit, ChangeFeedOrder order, CancellationToken cancellationToken = default)
    {
        EnsureArg.EnumIsDefined(order, nameof(order));

        if (range != TimeRange.MaxValue && order == ChangeFeedOrder.Sequence)
            throw new InvalidOperationException(DicomSqlServerResource.InvalidChangeFeedQuery);

        var results = new List<ChangeFeedEntry>();

        using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

        switch (order)
        {
            case ChangeFeedOrder.Sequence:
                VLatest.GetChangeFeedV6.PopulateCommand(sqlCommandWrapper, limit, offset);
                break;
            case ChangeFeedOrder.Time:
                VLatest.GetChangeFeedByTime.PopulateCommand(sqlCommandWrapper, range.Start, range.End, limit, offset);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(order));
        }

        using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            (long rSeq, DateTimeOffset rTimestamp, int rAction, string rPartitionName, string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid, long oWatermark, long? cWatermark) = reader.ReadRow(
                VLatest.ChangeFeed.Sequence,
                VLatest.ChangeFeed.Timestamp,
                VLatest.ChangeFeed.Action,
                VLatest.Partition.PartitionName,
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
                ConvertWatermarkToCurrentState(oWatermark, cWatermark),
                rPartitionName));
        }

        return results;
    }

    public override async Task<ChangeFeedEntry> GetChangeFeedLatestAsync(ChangeFeedOrder order, CancellationToken cancellationToken)
    {
        EnsureArg.EnumIsDefined(order, nameof(order));

        using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

        switch (order)
        {
            case ChangeFeedOrder.Sequence:
                VLatest.GetChangeFeedLatestV6.PopulateCommand(sqlCommandWrapper);
                break;
            case ChangeFeedOrder.Time:
                VLatest.GetChangeFeedLatestByTime.PopulateCommand(sqlCommandWrapper);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(order));
        }

        using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            (long rSeq, DateTimeOffset rTimestamp, int rAction, string rPartitionName, string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid, long oWatermark, long? cWatermark) = reader.ReadRow(
                VLatest.ChangeFeed.Sequence,
                VLatest.ChangeFeed.Timestamp,
                VLatest.ChangeFeed.Action,
                VLatest.Partition.PartitionName,
                VLatest.ChangeFeed.StudyInstanceUid,
                VLatest.ChangeFeed.SeriesInstanceUid,
                VLatest.ChangeFeed.SopInstanceUid,
                VLatest.ChangeFeed.OriginalWatermark,
                VLatest.ChangeFeed.CurrentWatermark);

            return new ChangeFeedEntry(
                rSeq,
                rTimestamp,
                (ChangeFeedAction)rAction,
                rStudyInstanceUid,
                rSeriesInstanceUid,
                rSopInstanceUid,
                oWatermark,
                cWatermark,
                ConvertWatermarkToCurrentState(oWatermark, cWatermark),
                rPartitionName);
        }

        return null;
    }
}
