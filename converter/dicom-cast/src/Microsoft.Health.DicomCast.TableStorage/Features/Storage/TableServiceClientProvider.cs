// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core;
using Microsoft.Health.DicomCast.TableStorage.Configs;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage
{
    public class TableServiceClientProvider : IHostedService, IRequireInitializationOnFirstRequest, IDisposable
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly RetryableInitializationOperation _initializationOperation;
        private readonly TableDataStoreConfiguration _tableDataStoreConfiguration;

        public Dictionary<string, string> TableList { get; }

        public TableServiceClientProvider(
            TableServiceClient tableServiceClient,
            ITableServiceClientInitializer tableServiceClientInitializer,
            IOptions<TableDataStoreConfiguration> tableDataStoreConfiguration,
            ILogger<TableServiceClientProvider> logger)
        {
            EnsureArg.IsNotNull(tableServiceClient, nameof(tableServiceClient));
            EnsureArg.IsNotNull(tableServiceClientInitializer, nameof(tableServiceClientInitializer));
            EnsureArg.IsNotNull(tableDataStoreConfiguration?.Value, nameof(tableDataStoreConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _tableDataStoreConfiguration = tableDataStoreConfiguration?.Value;

            TableList = new Dictionary<string, string>();
            InitializeTableNames();

            _tableServiceClient = tableServiceClient;
            _initializationOperation = new RetryableInitializationOperation(
                () => tableServiceClientInitializer.InitializeDataStoreAsync(_tableServiceClient, TableList));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // The result is ignored and will be awaited in EnsureInitialized(). Exceptions are logged within DocumentClientInitializer.
            _ = _initializationOperation.EnsureInitialized().AsTask();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Returns a task representing the initialization operation. Once completed,
        /// this method will always return a completed task. If the task fails, the method
        /// can be called again to retry the operation.
        /// </summary>
        /// <returns>A task representing the initialization operation.</returns>
        public async Task EnsureInitialized() => await _initializationOperation.EnsureInitialized();

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _initializationOperation.Dispose();
            }
        }

        public TableServiceClient GetTableServiceClient()
        {
            if (!_initializationOperation.IsInitialized)
            {
                _initializationOperation.EnsureInitialized().AsTask().GetAwaiter().GetResult();
            }

            return _tableServiceClient;
        }

        private void InitializeTableNames()
        {
            foreach (var table in Constants.AllTables)
            {
                TableList.Add(table, $"{_tableDataStoreConfiguration.TableNamePrefix}{table}");
            }
        }
    }
}
