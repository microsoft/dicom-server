// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using EnsureThat;
using Microsoft.Health.DicomCast.Core.Features.State;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage.Models.Entities;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage.Models
{
    public class TableSyncStateStore : ISyncStateStore
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly Dictionary<string, string> _tableList;

        public TableSyncStateStore(TableServiceClientProvider tableServiceClientProvider)
        {
            EnsureArg.IsNotNull(tableServiceClientProvider, nameof(tableServiceClientProvider));

            _tableServiceClient = tableServiceClientProvider.GetTableServiceClient();
            _tableList = tableServiceClientProvider.TableList;
        }

        public async Task<SyncState> ReadAsync(CancellationToken cancellationToken = default)
        {
            TableClient tableClient = _tableServiceClient.GetTableClient(_tableList[Constants.SyncStateTableName]);

            try
            {
                var entity = await tableClient.GetEntityAsync<SyncStateEntity>(Constants.SyncStatePartitionKey, Constants.SyncStateRowKey, cancellationToken: cancellationToken);
                return new SyncState(entity.Value.SyncedSequence, entity.Value.Timestamp.Value);
            }
            catch (RequestFailedException)
            {
                return SyncState.CreateInitialSyncState();
            }
        }

        public async Task UpdateAsync(SyncState state, CancellationToken cancellationToken = default)
        {
            TableClient tableClient = _tableServiceClient.GetTableClient(_tableList[Constants.SyncStateTableName]);
            var entity = new SyncStateEntity(state);

            await tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
        }
    }
}
