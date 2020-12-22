// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DicomCast.TableStorage.Configs;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage
{
#pragma warning disable CA1812
    internal class TableClientInitializer : ITableClientIntializer
    {
        private readonly ILogger<TableClientInitializer> _logger;

        public TableClientInitializer(ILogger<TableClientInitializer> logger)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            _logger = logger;
        }

        /// <inheritdoc />
        public CloudTableClient CreateTableClient(TableDataStoreConfiguration configuration)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            _logger.LogInformation("Creating TableClient instance");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(configuration.ConnectionString);

            // TODO: Add retry policy for acessing the table storage
            var tableRequestOptions = new TableRequestOptions();

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());

            return tableClient;
        }

        public async Task IntializeDataStoreAsync(CloudTableClient client, TableDataStoreConfiguration configuration)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            try
            {
                _logger.LogInformation("Initializing Table Storage and tables");
                foreach (string tableName in Constants.AllTables)
                {
                    CloudTable table = client.GetTableReference(tableName);
                    if (await table.CreateIfNotExistsAsync())
                    {
                        _logger.LogInformation("Created Table named: {0}", tableName);
                    }
                    else
                    {
                        _logger.LogInformation("Table {0} already exists", tableName);
                    }
                }

                _logger.LogInformation("Table Storage and tables successfully initialized");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Table Storage and table initialization failed");
                throw;
            }
        }
    }
#pragma warning restore CA1812
}
