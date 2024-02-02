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
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Blob.Features.Health;

internal class DicomConnectedStoreHealthCheck : IHealthCheck
{
    private readonly string _degradedDescription = "The health of the connected store has degraded.";
    private readonly string _testContent = "Test content.";

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
        BlockBlobClient healthCheckBlobClient = containerClient.GetBlockBlobClient(Path.Combine(_externalStoreOptions.StorageDirectory, $"{_externalStoreOptions.HealthCheckFilePath}{Guid.NewGuid()}.txt"));

        _logger.LogInformation("Attempting to write, read, and delete file {FileName}.", healthCheckBlobClient.Name);

        try
        {
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
            // Remove in WI #114591
            await TryDeleteOldBlob(containerClient);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Do not fail on clean up")]
    private async Task TryDeleteOldBlob(BlobContainerClient blobContainerClient)
    {
        try
        {
            BlockBlobClient blockBlobClient = blobContainerClient.GetBlockBlobClient($"{_externalStoreOptions.StorageDirectory}healthCheck/health.txt");
            await blockBlobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }
        catch (Exception)
        {
            // do not throw if cleaning up the previous blob fails
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
