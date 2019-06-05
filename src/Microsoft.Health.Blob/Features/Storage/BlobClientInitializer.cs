// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.RetryPolicies;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Blob.Features.Storage
{
    internal class BlobClientInitializer : IBlobClientInitializer
    {
        private readonly IBlobClientTestProvider _testProvider;
        private readonly ILogger<BlobClientInitializer> _logger;

        public BlobClientInitializer(IBlobClientTestProvider testProvider, ILogger<BlobClientInitializer> logger)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(testProvider, nameof(testProvider));

            _testProvider = testProvider;
            _logger = logger;
        }

        /// <inheritdoc />
        public CloudBlobClient CreateBlobClient(BlobDataStoreConfiguration configuration)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            _logger.LogInformation("Creating BlobClient instance for {connectionString}", configuration.ConnectionString);

            var storageAccount = CloudStorageAccount.Parse(configuration.ConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Configure the blob client default request options and retry logic
            blobClient.DefaultRequestOptions.RetryPolicy = new ExponentialRetry(
                deltaBackoff: TimeSpan.FromSeconds(configuration.RequestOptions.ExponentialRetryBackoffDeltaInSeconds),
                maxAttempts: configuration.RequestOptions.ExponentialRetryMaxAttempts);
            blobClient.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromMinutes(configuration.RequestOptions.ServerTimeoutInMinutes);
            blobClient.DefaultRequestOptions.ServerTimeout = TimeSpan.FromMinutes(configuration.RequestOptions.ServerTimeoutInMinutes);
            blobClient.DefaultRequestOptions.ParallelOperationThreadCount = configuration.RequestOptions.ParallelOperationThreadCount;

            return blobClient;
        }

        /// <inheritdoc />
        public async Task InitializeDataStoreAsync(CloudBlobClient client, BlobDataStoreConfiguration configuration, IEnumerable<IBlobContainerInitializer> containerInitializers)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(containerInitializers, nameof(containerInitializers));

            try
            {
                _logger.LogInformation("Initializing Blob Storage {connectionString} and containers", configuration.ConnectionString);

                foreach (IBlobContainerInitializer collectionInitializer in containerInitializers)
                {
                    await collectionInitializer.InitializeContainerAsync(client);
                }

                _logger.LogInformation("Blob Storage and containers successfully initialized");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Blob Storage and containers initialization failed");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task OpenBlobClientAsync(CloudBlobClient client, BlobDataStoreConfiguration configuration, BlobContainerConfiguration blobContainerConfiguration)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(blobContainerConfiguration, nameof(blobContainerConfiguration));

            _logger.LogInformation("Opening blob client connection to container {containerName}", blobContainerConfiguration.ContainerName);

            try
            {
                await _testProvider.PerformTestAsync(client, configuration, blobContainerConfiguration);

                _logger.LogInformation("Established blob client connection to container {containerName}", blobContainerConfiguration.ContainerName);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Failed to connect to blob client container {containerName}", blobContainerConfiguration.ContainerName);
                throw;
            }
        }
    }
}
