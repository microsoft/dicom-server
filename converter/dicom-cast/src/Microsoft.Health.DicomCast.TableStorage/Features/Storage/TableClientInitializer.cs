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
    public class TableClientInitializer : ITableClientInitializer
    {
        private readonly ILogger<TableClientInitializer> _logger;
        private readonly TableDataStoreConfiguration _configuration;

        public TableClientInitializer(
            ILogger<TableClientInitializer> logger,
            TableDataStoreConfiguration configuration)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            _logger = logger;
            _configuration = configuration;
        }

        /// <inheritdoc />
        public CloudTableClient CreateTableClient()
        {
            _logger.LogInformation("Creating TableClient instance");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_configuration.ConnectionString);

            // TODO: Add retry policy for acessing the table storage
            var tableRequestOptions = new TableRequestOptions();

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());

            return tableClient;
        }

        /// <inheritdoc />
        public async Task IntializeDataStoreAsync(CloudTableClient client)
        {
            EnsureArg.IsNotNull(client, nameof(client));

            try
            {
                _logger.LogInformation("Initializing Table Storage and tables");
                foreach (string tableName in Constants.AllTables)
                {
                    CloudTable table = client.GetTableReference(tableName);
                    if (await table.CreateIfNotExistsAsync())
                    {
                        _logger.LogInformation("Created Table named '{TableName}'", tableName);
                    }
                    else
                    {
                        _logger.LogInformation("Table '{TableName}' already exists", tableName);
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
}
