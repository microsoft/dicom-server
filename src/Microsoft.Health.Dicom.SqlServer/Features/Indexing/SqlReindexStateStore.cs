// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Indexing
{
    internal class SqlReindexStateStore : IReindexStateStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

        public SqlReindexStateStore(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
        }

        public Task CompleteReindexAsync(string operationId, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<ReindexEntry>> GetReindexEntriesAsync(string operationId, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public async Task<ReindexOperation> PrepareReindexingAsync(IReadOnlyList<int> tagKeys, string operationId, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(tagKeys, nameof(tagKeys));
            EnsureArg.IsNotEmptyOrWhiteSpace(operationId, nameof(operationId));

            ReindexOperation result = new ReindexOperation();
            result.OperationId = operationId;
            List<ExtendedQueryTagStoreEntry> storeEntries = new List<ExtendedQueryTagStoreEntry>();
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.PrepareReindexing.PopulateCommand(sqlCommandWrapper, tagKeys.Select(x => new PrepareReindexingTableTypeV1Row(x)), operationId);

                try
                {
                    using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (
                            int rTagKey,
                            string rTagPath,
                            string rTagVR,
                            string rTagPrivateCreator,
                            byte rTagLevel,
                            byte rTagStatus,
                            string rOperationid,
                            byte rReindexStatus,
                            long? rStartWatermark,
                            long? rEndWatermark
                        ) = reader.ReadRow(
                           VLatest.ExtendedQueryTag.TagKey,
                           VLatest.ExtendedQueryTag.TagPath,
                           VLatest.ExtendedQueryTag.TagVR,
                           VLatest.ExtendedQueryTag.TagPrivateCreator,
                           VLatest.ExtendedQueryTag.TagLevel,
                           VLatest.ExtendedQueryTag.TagStatus,
                           VLatest.ReindexState.OperationId,
                           VLatest.ReindexState.ReindexStatus,
                           VLatest.ReindexState.StartWatermark,
                           VLatest.ReindexState.EndWatermark);
                        storeEntries.Add(new ExtendedQueryTagStoreEntry(rTagKey, rTagPath, rTagVR, rTagPrivateCreator, (QueryTagLevel)rTagLevel, (ExtendedQueryTagStatus)rTagStatus));
                        result.StartWatermark = rStartWatermark;
                        result.EndWatermark = rEndWatermark;
                    }
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.PreconditionFailed:
                            throw new ExtendedQueryTagBusyException(DicomSqlServerResource.ExtendedQueryTagsAreBusy);
                        default:
                            throw new DataStoreException(ex);
                    }
                }
                result.StoreEntries = storeEntries;

                return result;
            }
        }

        public Task UpdateReindexProgressAsync(string operationId, long endWatermark, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
