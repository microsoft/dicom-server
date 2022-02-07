// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage
{
    public class TableServiceClientInitializer : ITableServiceClientInitializer
    {
        private readonly ILogger<TableServiceClientInitializer> _logger;

        public TableServiceClientInitializer(
            ILogger<TableServiceClientInitializer> logger)
        {
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        /// <inheritdoc />
        public async Task InitializeDataStoreAsync(TableServiceClient tableServiceClient, Dictionary<string, string> tableList)
        {
            EnsureArg.IsNotNull(tableServiceClient, nameof(tableServiceClient));
            EnsureArg.IsNotNull(tableList, nameof(tableList));

            try
            {
                _logger.LogInformation("Initializing Table Storage and tables");

                foreach (string tableName in tableList.Values)
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
