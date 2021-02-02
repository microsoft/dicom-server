// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Health.DicomCast.Core.Exceptions;
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

            TableResult result;

            try
            {
                result = await table.ExecuteAsync(retrieveOperation);
            }
            catch (Exception ex)
            {
                throw new DataStoreException(ex);
            }

            SyncStateEntity syncState = result.Result as SyncStateEntity;

            if (syncState != null)
            {
                return new SyncState(syncState.SyncedSequence, syncState.Timestamp);
            }

            return SyncState.CreateInitialSyncState();
        }

        public async Task UpdateAsync(SyncState state, CancellationToken cancellationToken = default)
        {
            CloudTable table = _client.GetTableReference(Constants.SyncStateTableName);
            TableEntity entity = new SyncStateEntity(state);

            TableOperation operation = TableOperation.InsertOrReplace(entity);
            try
            {
                await table.ExecuteAsync(operation, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new DataStoreException(ex);
            }
        }
    }
}
