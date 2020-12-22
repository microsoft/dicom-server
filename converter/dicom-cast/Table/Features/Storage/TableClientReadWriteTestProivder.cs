// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Health.DicomCast.TableStorage.Configs;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage.Entities;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage
{
    public class TableClientReadWriteTestProivder : ITableClientTestProvider
    {
        private const string TestPartitionKey = "testpartition";
        private const string TestRowKey = "testrow";
        private const string TestData = "testdata";
        private const string TestTable = "testTable";

        public async Task PerformTestAsync(CloudTableClient client, TableDataStoreConfiguration configuration, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            CloudTable table = client.GetTableReference(TestTable);
            await table.CreateIfNotExistsAsync();

            HealthEntity entity = new HealthEntity(TestPartitionKey, TestRowKey);
            entity.Data = TestData;

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

            TableResult result = await table.ExecuteAsync(insertOrMergeOperation);

            TableOperation retrieveOperation = TableOperation.Retrieve<HealthEntity>(TestPartitionKey, TestRowKey);
            result = await table.ExecuteAsync(retrieveOperation);

            TableOperation deleteOperation = TableOperation.Delete(entity);
            result = await table.ExecuteAsync(deleteOperation);

            await table.DeleteAsync();
        }
    }
}
