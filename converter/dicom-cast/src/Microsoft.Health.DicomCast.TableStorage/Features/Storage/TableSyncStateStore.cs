// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Health.DicomCast.Core.Features.State;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage.Models.Entities;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage.Models
{
    public class TableSyncStateStore : ISyncStateStore
    {
        private readonly CloudTableClient _client;

        public TableSyncStateStore(CloudTableClient client)
        {
            EnsureArg.IsNotNull(client, nameof(client));

            _client = client;
        }

        public async Task<SyncState> ReadAsync(CancellationToken cancellationToken = default)
        {
            CloudTable table = _client.GetTableReference(Constants.SyncStateTableName);
            TableOperation retrieveOperation = TableOperation.Retrieve<SyncStateEntity>(Constants.SyncStatePartitionKey, Constants.SyncStateRowKey);

            TableResult result = await table.ExecuteAsync(retrieveOperation, cancellationToken);

            return result.Result is SyncStateEntity syncState
                ? new SyncState(syncState.SyncedSequence, syncState.Timestamp)
                : SyncState.CreateInitialSyncState();
        }

        public async Task UpdateAsync(SyncState state, CancellationToken cancellationToken = default)
        {
            CloudTable table = _client.GetTableReference(Constants.SyncStateTableName);
            TableEntity entity = new SyncStateEntity(state);

            TableOperation operation = TableOperation.InsertOrReplace(entity);

            await table.ExecuteAsync(operation, cancellationToken);
        }
    }
}
