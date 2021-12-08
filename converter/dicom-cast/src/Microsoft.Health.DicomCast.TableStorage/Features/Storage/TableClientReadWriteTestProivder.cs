// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using EnsureThat;
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
        public async Task PerformTestAsync(TableServiceClient testServiceClient, TableDataStoreConfiguration configuration, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(testServiceClient, nameof(testServiceClient));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            await testServiceClient.CreateTableIfNotExistsAsync(TestTable, cancellationToken: cancellationToken);

            var tableClient = testServiceClient.GetTableClient(TestTable);
            var entity = new HealthEntity(TestPartitionKey, TestRowKey) { Data = TestData };

            await tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);

            await tableClient.GetEntityAsync<HealthEntity>(TestPartitionKey, TestRowKey, cancellationToken: cancellationToken);

            await tableClient.DeleteEntityAsync(TestPartitionKey, TestRowKey, cancellationToken: cancellationToken);

            await testServiceClient.DeleteTableAsync(TestTable, cancellationToken);
        }
    }
}
