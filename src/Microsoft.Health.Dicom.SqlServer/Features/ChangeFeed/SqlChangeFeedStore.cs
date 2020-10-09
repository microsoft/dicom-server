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
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed
{
    public class SqlChangeFeedStore : IChangeFeedStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
        private readonly ILogger<SqlChangeFeedStore> _logger;

        public SqlChangeFeedStore(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<SqlChangeFeedStore> logger)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
            _logger = logger;
        }

        public async Task<ChangeFeedEntry> GetChangeFeedLatestAsync(CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetChangeFeedLatest.PopulateCommand(sqlCommandWrapper);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
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

                        return new ChangeFeedEntry(
                                rSeq,
                                rTimestamp,
                                (ChangeFeedAction)rAction,
                                rStudyInstanceUid,
                                rSeriesInstanceUid,
                                rSopInstanceUid,
                                oWatermark,
                                cWatermark,
                                ConvertWatermarkToCurrentState(oWatermark, cWatermark));
                    }
                }
            }

            return null;
        }

        public async Task<IReadOnlyCollection<ChangeFeedEntry>> GetChangeFeedAsync(long offset, int limit, CancellationToken cancellationToken)
        {
            var results = new List<ChangeFeedEntry>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetChangeFeed.PopulateCommand(sqlCommandWrapper, limit, offset);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
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
                }
            }

            return results;
        }

        private static ChangeFeedState ConvertWatermarkToCurrentState(long originalWatermark, long? currentWatermak)
        {
            if (currentWatermak == null)
            {
                return ChangeFeedState.Deleted;
            }

            if (currentWatermak != originalWatermark)
            {
                return ChangeFeedState.Replaced;
            }

            return ChangeFeedState.Current;
        }
    }
}
