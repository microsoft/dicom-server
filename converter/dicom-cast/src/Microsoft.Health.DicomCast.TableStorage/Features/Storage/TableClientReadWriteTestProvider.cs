// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using EnsureThat;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage.Entities;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage
{
    public class TableClientReadWriteTestProvider : ITableClientTestProvider
    {
        private const string TestPartitionKey = "testpartition";
        private const string TestRowKey = "testrow";
        private const string TestData = "testdata";
        private const string TestTable = "testTable";

        private readonly TableServiceClient _testServiceClient;

        public TableClientReadWriteTestProvider(TableServiceClient testServiceClient)
        {
            _testServiceClient = EnsureArg.IsNotNull(testServiceClient, nameof(testServiceClient));
        }

        /// <inheritdoc/>
        public async Task PerformTestAsync(CancellationToken cancellationToken = default)
        {
            await _testServiceClient.CreateTableIfNotExistsAsync(TestTable, cancellationToken: cancellationToken);

            var tableClient = _testServiceClient.GetTableClient(TestTable);
            var entity = new HealthEntity(TestPartitionKey, TestRowKey) { Data = TestData };

            await tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);

            await tableClient.GetEntityAsync<HealthEntity>(TestPartitionKey, TestRowKey, cancellationToken: cancellationToken);

            await tableClient.DeleteEntityAsync(TestPartitionKey, TestRowKey, cancellationToken: cancellationToken);
        }
    }
}
