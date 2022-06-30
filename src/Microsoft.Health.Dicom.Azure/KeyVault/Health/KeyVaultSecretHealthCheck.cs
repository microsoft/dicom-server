// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Dicom.Azure.KeyVault.Health;

internal sealed class KeyVaultSecretHealthCheck : IHealthCheck
{
    private readonly SecretClient _client;
    private readonly ILogger _logger;

    public KeyVaultSecretHealthCheck(SecretClient client, ILogger<KeyVaultSecretHealthCheck> logger)
    {
        _client = EnsureArg.IsNotNull(client, nameof(client));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        // One page is sufficient for testing connectivity
        _logger.LogInformation($"Starting {nameof(KeyVaultSecretHealthCheck)}.");
        await _client.GetPropertiesOfSecretsAsync(cancellationToken).AsPages(pageSizeHint: 1).FirstOrDefaultAsync(cancellationToken);

        _logger.LogInformation("Successfully connected to Azure Key Vault.");
        return HealthCheckResult.Healthy();
    }
}
