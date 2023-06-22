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
using Microsoft.Health.Dicom.SqlServer.Extensions;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed;

internal class SqlChangeFeedStoreV39 : SqlChangeFeedStoreV36
{
    public override SchemaVersion Version => SchemaVersion.V39;

    public SqlChangeFeedStoreV39(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
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
                VLatest.GetChangeFeedV39.PopulateCommand(sqlCommandWrapper, limit, offset);
                break;
            case ChangeFeedOrder.Time:
                VLatest.GetChangeFeedByTimeV39.PopulateCommand(sqlCommandWrapper, range.Start, range.End, limit, offset);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(order));
        }

        using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            (long rSeq, DateTimeOffset rTimestamp, int rAction, string rPartitionName, string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid, long oWatermark, long? cWatermark, string filePath) = reader
                .ReadRow(
                    VLatest.ChangeFeed.Sequence,
                    VLatest.ChangeFeed.Timestamp,
                    VLatest.ChangeFeed.Action,
                    VLatest.Partition.PartitionName,
                    VLatest.ChangeFeed.StudyInstanceUid,
                    VLatest.ChangeFeed.SeriesInstanceUid,
                    VLatest.ChangeFeed.SopInstanceUid,
                    VLatest.ChangeFeed.OriginalWatermark,
                    VLatest.ChangeFeed.CurrentWatermark,
                    VLatest.FileProperty.FilePath.AsNullable());

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
                rPartitionName,
                filePath: filePath));
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
                VLatest.GetChangeFeedLatestV39.PopulateCommand(sqlCommandWrapper);
                break;
            case ChangeFeedOrder.Time:
                VLatest.GetChangeFeedLatestByTimeV39.PopulateCommand(sqlCommandWrapper);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(order));
        }

        using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            (long rSeq, DateTimeOffset rTimestamp, int rAction, string rPartitionName, string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid, long oWatermark, long? cWatermark, string filePath) = reader
                .ReadRow(
                    VLatest.ChangeFeed.Sequence,
                    VLatest.ChangeFeed.Timestamp,
                    VLatest.ChangeFeed.Action,
                    VLatest.Partition.PartitionName,
                    VLatest.ChangeFeed.StudyInstanceUid,
                    VLatest.ChangeFeed.SeriesInstanceUid,
                    VLatest.ChangeFeed.SopInstanceUid,
                    VLatest.ChangeFeed.OriginalWatermark,
                    VLatest.ChangeFeed.CurrentWatermark,
                    VLatest.FileProperty.FilePath.AsNullable());

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
                rPartitionName,
                filePath: filePath);
        }

        return null;
    }
}
