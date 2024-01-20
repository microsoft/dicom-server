// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Dicom.Blob.Extensions;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Utilities;

namespace Microsoft.Health.Dicom.Blob.Features.Health;

internal class DicomConnectedStoreHealthCheck : IHealthCheck
{
    private readonly string _degradedDescription = "The health of the connected store has degraded.";
    private readonly string _testContent = "Test content.";
    private readonly string _leaseBlobContent = "lease";

    private readonly ExternalBlobDataStoreConfiguration _externalStoreOptions;
    private readonly IBlobClient _blobClient;
    private readonly ILogger<DicomConnectedStoreHealthCheck> _logger;

    /// <summary>
    /// Validate health of connected blob store
    /// </summary>
    /// <param name="blobClient">the blob client</param>
    /// <param name="externalStoreOptions">external store options</param>
    /// <param name="logger">logger</param>
    public DicomConnectedStoreHealthCheck(IBlobClient blobClient, IOptions<ExternalBlobDataStoreConfiguration> externalStoreOptions, ILogger<DicomConnectedStoreHealthCheck> logger)
    {
        _externalStoreOptions = EnsureArg.IsNotNull(externalStoreOptions?.Value, nameof(externalStoreOptions));
        _blobClient = EnsureArg.IsNotNull(blobClient, nameof(blobClient));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking the health of the connected store.");

        BlobContainerClient containerClient = _blobClient.BlobContainerClient;

        BlockBlobClient healthCheckBlobClient = containerClient.GetBlockBlobClient($"{_externalStoreOptions.StorageDirectory}{_externalStoreOptions.HealthCheckFileName}");
        BlockBlobClient leaseBlobClient = containerClient.GetBlockBlobClient($"{_externalStoreOptions.StorageDirectory}{_externalStoreOptions.HealthCheckLeaseFileName}");
        BlobLeaseClient leaseBlobLeaseClient = leaseBlobClient.GetBlobLeaseClient();

        try
        {
            if (!await leaseBlobClient.ExistsAsync(cancellationToken))
            {
                // create the blob to get a lease on if it does not exist
                using Stream leaseStream = new MemoryStream(Encoding.UTF8.GetBytes(_leaseBlobContent));
                await leaseBlobClient.UploadAsync(leaseStream, cancellationToken: cancellationToken);
            }

            // acquire lease on lease blob to ensure there is no conflict writing/reading/deleting the blob
            await TryAcquireLease(leaseBlobLeaseClient, cancellationToken);

            using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(_testContent));

            // test blob/write
            await healthCheckBlobClient.UploadAsync(stream, cancellationToken: cancellationToken);

            // test blob/read and blob/metadata/read
            await healthCheckBlobClient.DownloadContentAsync(cancellationToken);

            // test blob/delete
            await healthCheckBlobClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, new BlobRequestConditions(), cancellationToken);

            return HealthCheckResult.Healthy("Successfully connected.");
        }
        catch (RequestFailedException rfe) when (rfe.IsConnectedStoreCustomerError())
        {
            return GetConnectedStoreDegradedResult(rfe);
        }
        catch (Exception ex) when (ex.IsStorageAccountUnknownHostError())
        {
            return GetConnectedStoreDegradedResult(ex);
        }
        finally
        {
            await TryReleaseLease(leaseBlobLeaseClient, cancellationToken);
        }
    }

    public static async Task TryReleaseLease(BlobLeaseClient leaseBlobLeaseClient, CancellationToken cancellationToken)
    {
        try
        {
            await leaseBlobLeaseClient.ReleaseAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException)
        {
            // do not thrown an error if this fails since this is not part of what the health check is validation
            // If it fails to release the lease, it will expire after 15 seconds regardless.
        }
    }

    public static async Task TryAcquireLease(BlobLeaseClient blobLeaseClient, CancellationToken cancellationToken)
    {
        int retry = 0;

        while (retry < 15)
        {
            try
            {
                await blobLeaseClient.AcquireAsync(TimeSpan.FromSeconds(15), cancellationToken: cancellationToken);
                break;
            }
            catch (RequestFailedException rfe) when (
                rfe.ErrorCode == BlobErrorCode.LeaseAlreadyPresent ||
                rfe.ErrorCode == BlobErrorCode.LeaseIsBreakingAndCannotBeAcquired)
            {
                // retry and wait for lease to be available
                retry++;
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private HealthCheckResult GetConnectedStoreDegradedResult(Exception exception)
    {
        _logger.LogInformation(exception, "The connected store health check failed due to a client issue.");

        return new HealthCheckResult(
            HealthStatus.Degraded,
            _degradedDescription,
            exception,
            new Dictionary<string, object> { { "Reason", HealthStatusReason.ConnectedStoreDegraded.ToString() } });
    }
}
