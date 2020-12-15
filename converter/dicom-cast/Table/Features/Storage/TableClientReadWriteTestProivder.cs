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

        public async Task PerformTestAsync(CloudTableClient client, TableDataStoreConfiguration configuration, TableConfiguration tableConfiguration, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(tableConfiguration, nameof(tableConfiguration));

            CloudTable table = client.GetTableReference(tableConfiguration.TableName);
            HealthEntity entity = new HealthEntity(TestPartitionKey, TestRowKey);
            entity.Data = TestData;

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

            TableResult result = await table.ExecuteAsync(insertOrMergeOperation);

            TableOperation retrieveOperation = TableOperation.Retrieve<HealthEntity>(TestPartitionKey, TestRowKey);
            result = await table.ExecuteAsync(retrieveOperation);

            TableOperation deleteOperation = TableOperation.Delete(entity);
            result = await table.ExecuteAsync(deleteOperation);
        }
    }
}
