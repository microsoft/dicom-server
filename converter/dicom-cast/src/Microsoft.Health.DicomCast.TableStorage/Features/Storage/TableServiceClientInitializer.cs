// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Identity;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DicomCast.TableStorage.Configs;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage
{
    public class TableServiceClientInitializer : ITableServiceClientInitializer
    {
        private readonly ILogger<TableServiceClientInitializer> _logger;
        private readonly TableDataStoreConfiguration _configuration;

        public TableServiceClientInitializer(
            ILogger<TableServiceClientInitializer> logger,
            TableDataStoreConfiguration configuration)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            _logger = logger;
            _configuration = configuration;
        }

        /// <inheritdoc />
        public TableServiceClient CreateTableServiceClient()
        {
            // TODO: Add retry policy for accessing the table storage
            if (!string.IsNullOrWhiteSpace(_configuration.ConnectionString))
            {
                return new TableServiceClient(_configuration.ConnectionString);
            }

            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = _configuration.ManagedIdentityClientId });
            return new TableServiceClient(_configuration.EndpointUri, credential);
        }

        /// <inheritdoc />
        public async Task InitializeDataStoreAsync(TableServiceClient tableServiceClient)
        {
            EnsureArg.IsNotNull(tableServiceClient, nameof(tableServiceClient));

            try
            {
                _logger.LogInformation("Initializing Table Storage and tables");
                foreach (string tableName in Constants.AllTables)
                {
                    if (await tableServiceClient.CreateTableIfNotExistsAsync(tableName) != null)
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
