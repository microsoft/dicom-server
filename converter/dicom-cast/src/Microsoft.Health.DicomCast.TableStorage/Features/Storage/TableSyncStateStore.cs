// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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

        public TableSyncStateStore(TableServiceClient tableServiceClient)
        {
            EnsureArg.IsNotNull(tableServiceClient, nameof(tableServiceClient));

            _tableServiceClient = tableServiceClient;
        }

        public async Task<SyncState> ReadAsync(CancellationToken cancellationToken = default)
        {
            TableClient tableClient = _tableServiceClient.GetTableClient(Constants.SyncStateTableName);

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
            TableClient tableClient = _tableServiceClient.GetTableClient(Constants.SyncStateTableName);
            var entity = new SyncStateEntity(state);

            await tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
        }
    }
}
