// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Blob.Features.Health
{
    public class BlobHealthCheck : IHealthCheck
    {
        private readonly IScoped<CloudBlobClient> _client;
        private readonly BlobDataStoreConfiguration _configuration;
        private readonly BlobContainerConfiguration _blobContainerConfiguration;
        private readonly IBlobClientTestProvider _testProvider;
        private readonly ILogger<BlobHealthCheck> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobHealthCheck"/> class.
        /// </summary>
        /// <param name="client">The cloud blob client factory.</param>
        /// <param name="configuration">The CosmosDB configuration.</param>
        /// <param name="namedBlobContainerConfigurationAccessor">The IOptions accessor to get a named container configuration version.</param>
        /// <param name="containerConfigurationName">Name to get corresponding cfontainer configuration.</param>
        /// <param name="testProvider">The test provider.</param>
        /// <param name="logger">The logger.</param>
        public BlobHealthCheck(
            IScoped<CloudBlobClient> client,
            BlobDataStoreConfiguration configuration,
            IOptionsSnapshot<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            string containerConfigurationName,
            IBlobClientTestProvider testProvider,
            ILogger<BlobHealthCheck> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNullOrWhiteSpace(containerConfigurationName, nameof(containerConfigurationName));
            EnsureArg.IsNotNull(testProvider, nameof(testProvider));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _client = client;
            _configuration = configuration;
            _blobContainerConfiguration = namedBlobContainerConfigurationAccessor.Get(containerConfigurationName);
            _testProvider = testProvider;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Make a non-invasive query to make sure we can reach the data store.

                await _testProvider.PerformTestAsync(_client.Value, _configuration, _blobContainerConfiguration);

                return HealthCheckResult.Healthy("Successfully connected to the blob data store.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to the blob data store.");

                return HealthCheckResult.Unhealthy("Failed to connect to the blob data store.");
            }
        }
    }
}
