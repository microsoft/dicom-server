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

        /// <inheritdoc/>
        public async Task PerformTestAsync(CloudTableClient client, TableDataStoreConfiguration configuration, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            CloudTable table = client.GetTableReference(TestTable);
            await table.CreateIfNotExistsAsync(cancellationToken);

            var entity = new HealthEntity(TestPartitionKey, TestRowKey) { Data = TestData };

            var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

            await table.ExecuteAsync(insertOrMergeOperation, cancellationToken);

            var retrieveOperation = TableOperation.Retrieve<HealthEntity>(TestPartitionKey, TestRowKey);
            await table.ExecuteAsync(retrieveOperation, cancellationToken);

            var deleteOperation = TableOperation.Delete(entity);
            await table.ExecuteAsync(deleteOperation, cancellationToken);

            await table.DeleteAsync(cancellationToken);
        }
    }
}
