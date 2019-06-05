// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Blob.Features.Storage
{
    public class BlobContainerInitializer : IBlobContainerInitializer
    {
        private readonly string _containerName;
        private readonly ILogger<BlobContainerInitializer> _logger;

        public BlobContainerInitializer(string containerName, ILogger<BlobContainerInitializer> logger)
        {
            EnsureArg.IsNotNullOrWhiteSpace(containerName, nameof(containerName));
            EnsureArg.IsNotNull(logger, nameof(logger));

            // Use the Azure storage SDK to validate the container name; only specific values are allowed here (throws ArgumentException).
            // Check here for more information: https://blogs.msdn.microsoft.com/jmstall/2014/06/12/azure-storage-naming-rules/
            NameValidator.ValidateContainerName(containerName);

            _containerName = containerName;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<CloudBlobContainer> InitializeContainerAsync(CloudBlobClient blobClient)
        {
            EnsureArg.IsNotNull(blobClient, nameof(blobClient));

            CloudBlobContainer container = blobClient.GetContainerReference(_containerName);

            _logger.LogDebug("Creating blob container if not exits: {containerName}", _containerName);
            await container.CreateIfNotExistsAsync();

            return container;
        }
    }
}
