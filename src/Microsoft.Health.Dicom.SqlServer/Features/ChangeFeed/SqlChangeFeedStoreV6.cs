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
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed
{
    public class SqlChangeFeedStoreV6 : SqlChangeFeedStoreV5
    {
        public override SchemaVersion Version => SchemaVersion.V6;

        public SqlChangeFeedStoreV6(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
           : base(sqlConnectionWrapperFactory)
        {
        }

        public override async Task<ChangeFeedEntry> GetChangeFeedLatestAsync(CancellationToken cancellationToken)
        {
            using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand();

            VLatest.GetChangeFeedLatestV6.PopulateCommand(sqlCommandWrapper);

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

        public override async Task<IReadOnlyCollection<ChangeFeedEntry>> GetChangeFeedAsync(long offset, int limit, CancellationToken cancellationToken)
        {
            var results = new List<ChangeFeedEntry>();

            using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand();

            VLatest.GetChangeFeedV6.PopulateCommand(sqlCommandWrapper, limit, offset);

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
    }
}
