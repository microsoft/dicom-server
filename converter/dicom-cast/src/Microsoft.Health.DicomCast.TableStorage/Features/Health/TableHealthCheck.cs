// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Health
{
    public class TableHealthCheck : IHealthCheck
    {
        private readonly ITableClientTestProvider _testProvider;
        private readonly ILogger<TableHealthCheck> _logger;

        public TableHealthCheck(
            ITableClientTestProvider testProvider,
            ILogger<TableHealthCheck> logger)
        {
            EnsureArg.IsNotNull(testProvider, nameof(testProvider));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _testProvider = testProvider;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _testProvider.PerformTestAsync(cancellationToken);

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
