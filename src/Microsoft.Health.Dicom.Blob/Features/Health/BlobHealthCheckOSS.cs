// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Health;

namespace Microsoft.Health.Dicom.Blob.Features.Health;
public class BlobHealthCheckOSS : IHealthCheck
{
    private readonly BlobServiceClient _client;
    private readonly BlobContainerConfiguration _blobContainerConfiguration;
    private readonly ILogger<BlobHealthCheckOSS> _logger;

    private const string TestBlobName = "TestBlob";
    private const string TestBlobContent = "test-data";

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobHealthCheck"/> class.
    /// </summary>
    /// <param name="client">The cloud blob client factory.</param>
    /// <param name="namedBlobContainerConfigurationAccessor">The IOptions accessor to get a named container configuration version.</param>
    /// <param name="containerConfigurationName">Name to get corresponding container configuration.</param>
    /// <param name="logger">The logger.</param>
    public BlobHealthCheckOSS(
        BlobServiceClient client,
        IOptionsSnapshot<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        string containerConfigurationName,
        ILogger<BlobHealthCheckOSS> logger)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        EnsureArg.IsNotNullOrWhiteSpace(containerConfigurationName, nameof(containerConfigurationName));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _client = client;
        _blobContainerConfiguration = namedBlobContainerConfigurationAccessor.Get(containerConfigurationName);
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing health check.");

        try
        {
            BlobContainerClient containerClient = _client.GetBlobContainerClient(_blobContainerConfiguration.ContainerName);
            var blobClient = containerClient.GetBlobClient(TestBlobName);
            bool exists = await blobClient.ExistsAsync(cancellationToken);

            if (!exists)
            {
                using var content = new MemoryStream(Encoding.UTF8.GetBytes(TestBlobContent));
                await blobClient.UploadAsync(content, cancellationToken);
            }
            await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("Success");
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "KeyVaultEncryptionKeyNotFound")
        {
            return HealthCheckResult.Degraded(
                "Access to the customer-managed key has been lost",
                null,
                new Dictionary<string, object> { { "StorageCMKAccessLost", true } });
        }
        catch (RequestFailedException)
        {
            return HealthCheckResult.Degraded(
                "Failed to access the storage account",
                null,
                new Dictionary<string, object> { { "StorageAccountAccessDegraded", true } });
        }
    }
}
