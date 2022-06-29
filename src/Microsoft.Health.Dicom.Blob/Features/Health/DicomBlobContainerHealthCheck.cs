// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Health.Dicom.Blob.Features.Health;

internal sealed class DicomBlobContainerHealthCheck : IHealthCheck
{
    private readonly BlobServiceClient _client;
    private readonly string _containerName;

    public DicomBlobContainerHealthCheck(BlobServiceClient client, string containerName)
    {
        _client = EnsureArg.IsNotNull(client, nameof(client));
        _containerName = EnsureArg.IsNotNullOrWhiteSpace(containerName, nameof(containerName));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = _client.GetBlobContainerClient(_containerName);
        if (!await containerClient.ExistsAsync(cancellationToken))
        {
            return new HealthCheckResult(context.Registration.FailureStatus, description: $"Container '{_containerName}' not exists");
        }

        await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken);

        return HealthCheckResult.Healthy();
    }
}
