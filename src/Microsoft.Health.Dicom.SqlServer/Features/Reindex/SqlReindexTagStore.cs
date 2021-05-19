// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Reindex;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    /// <summary>
    ///  Sql version of IIndexDataStore.
    /// </summary>
    internal class SqlReindexTagStore : IReindexTagStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

        public SqlReindexTagStore(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
        }
        public async Task CompleteReindexAsync(long operationKey, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.CompleteReindex.PopulateCommand(sqlCommandWrapper, operationKey);

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

        public async Task<IReadOnlyList<ReindexTagStoreEntry>> GetTagsOnOperationAsync(long operationKey, CancellationToken cancellationToken = default)
        {
            var results = new List<ReindexTagStoreEntry>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetTagsOnOperation.PopulateCommand(sqlCommandWrapper, operationKey);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (int rTagKey, string rTagPath, string rTagVR, string rTagPrivateCreator, byte rTagLevel, byte rTagStatus, long rOperationKey, byte rStatus, long rEndWatarmark) =
                            reader.ReadRow(
                           VLatest.ExtendedQueryTag.TagKey,
                           VLatest.ExtendedQueryTag.TagPath,
                           VLatest.ExtendedQueryTag.TagVR,
                           VLatest.ExtendedQueryTag.TagPrivateCreator,
                           VLatest.ExtendedQueryTag.TagLevel,
                           VLatest.ExtendedQueryTag.TagStatus,
                           VLatest.TagReindexOperation.OperationKey,
                           VLatest.TagReindexOperation.Status,
                           VLatest.TagReindexOperation.EndWatermark);

                        ExtendedQueryTagStoreEntry entry = new ExtendedQueryTagStoreEntry(rTagKey, rTagPath, rTagVR, rTagPrivateCreator, (QueryTagLevel)rTagLevel, (ExtendedQueryTagStatus)rTagStatus);
                        results.Add(new ReindexTagStoreEntry() { EndWatarmark = rEndWatarmark, OperationKey = rOperationKey, QueryTagStoreEntry = entry, Status = (ReindexTagStoreStatus)rStatus });
                    }
                }
            }

            return results;
        }

        public async Task<IReadOnlyList<long>> GetWatermarksAsync(long operationKey, int topN, CancellationToken cancellationToken = default)
        {
            var results = new List<long>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetWatarmarks.PopulateCommand(sqlCommandWrapper, operationKey, topN);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        long rWatarmark = reader.ReadRow(VLatest.Instance.Watermark);
                        results.Add(rWatarmark);
                    }
                }
            }

            return results;
        }

        public async Task UpdateMaxWatermarkAsync(long operationKey, long maxWatarmark, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.UpdateMaxWatarmarks.PopulateCommand(sqlCommandWrapper, operationKey, maxWatarmark);

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
    }
}
