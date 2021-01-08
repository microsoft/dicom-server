// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core;
using Microsoft.Health.DicomCast.TableStorage.Configs;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage
{
    public class TableClientProvider : IHostedService, IRequireInitializationOnFirstRequest, IDisposable
    {
        private readonly CloudTableClient _cloudTableClient;
        private readonly RetryableInitializationOperation _initializationOperation;

        public TableClientProvider(
            TableDataStoreConfiguration tableDataStoreConfiguration,
            ITableClientInitializer tableClientIntilizer,
            ILogger<TableClientProvider> logger)
        {
            EnsureArg.IsNotNull(tableDataStoreConfiguration, nameof(tableDataStoreConfiguration));
            EnsureArg.IsNotNull(tableClientIntilizer, nameof(tableClientIntilizer));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _cloudTableClient = tableClientIntilizer.CreateTableClient();
            _initializationOperation = new RetryableInitializationOperation(
                () => tableClientIntilizer.IntializeDataStoreAsync(_cloudTableClient));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // The result is ignored and will be awaited in EnsureInitialized(). Exceptions are logged within DocumentClientInitializer.
            _ = _initializationOperation.EnsureInitialized();

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

        public CloudTableClient CreateTableClient()
        {
            if (!_initializationOperation.IsInitialized)
            {
                _initializationOperation.EnsureInitialized().GetAwaiter().GetResult();
            }

            return _cloudTableClient;
        }
    }
}
