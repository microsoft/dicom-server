// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Dicom.Blob.Features.Health;

internal sealed class DicomBlobContainerHealthCheck : IHealthCheck
{
    private readonly BlobServiceClient _client;
    private readonly DicomBlobContainerOptions _options;
    private readonly ILogger _logger;

    public DicomBlobContainerHealthCheck(BlobServiceClient client, IOptions<DicomBlobContainerOptions> options, ILogger<DicomBlobContainerHealthCheck> logger)
    {
        _client = EnsureArg.IsNotNull(client, nameof(client));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        _logger.LogInformation($"Starting {nameof(DicomBlobContainerHealthCheck)}.");
        await Task.WhenAll(
            CheckContainerAsync(_options.File, cancellationToken),
            CheckContainerAsync(_options.Metadata, cancellationToken));

        _logger.LogInformation("Successfully connected to Azure Blob Storage.");
        return HealthCheckResult.Healthy();
    }

    private Task CheckContainerAsync(string containerName, CancellationToken cancellationToken)
    {
        BlobContainerClient containerClient = _client.GetBlobContainerClient(containerName);
        return containerClient.GetPropertiesAsync(cancellationToken: cancellationToken);
    }
}
