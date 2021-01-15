// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DicomCast.TableStorage.Configs;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Health
{
    public class TableHealthCheck : IHealthCheck
    {
        private readonly CloudTableClient _client;
        private readonly TableDataStoreConfiguration _configuration;
        private readonly ITableClientTestProvider _testProvider;
        private readonly ILogger<TableHealthCheck> _logger;

        public TableHealthCheck(
            CloudTableClient client,
            TableDataStoreConfiguration configuration,
            ITableClientTestProvider testProvider,
            ILogger<TableHealthCheck> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(testProvider, nameof(testProvider));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _client = client;
            _configuration = configuration;
            _testProvider = testProvider;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _testProvider.PerformTestAsync(_client, _configuration, cancellationToken);

                return HealthCheckResult.Healthy("Successfully connected to the table data store.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to the table data store.");

                return HealthCheckResult.Unhealthy("Failed to connect to the table data store.");
            }
        }
    }
}
