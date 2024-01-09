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
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Dicom.Blob.Extensions;
using Microsoft.Health.Dicom.Blob.Features.Storage;

namespace Microsoft.Health.Dicom.Blob.Features.Health;

internal class DicomConnectedStoreHealthCheck : IHealthCheck
{
    private readonly string _degradedDescription = "The health of the connected store has degraded.";

    private readonly string _testContent = "Test content.";
    private readonly string _testFileName = "healthCheck.txt";

    private readonly IBlobClient _blobClient;

    public DicomConnectedStoreHealthCheck(IBlobClient blobClient)
    {
        _blobClient = EnsureArg.IsNotNull(blobClient, nameof(blobClient));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = _blobClient.BlobContainerClient;

        // Use GUID for directory name to avoid conflicts with customer files
        Guid directoryName = Guid.NewGuid();
        BlobClient blobClient = containerClient.GetBlobClient($"{directoryName}/{_testFileName}");
        try
        {
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(_testContent)))
            {
                await blobClient.UploadAsync(stream, cancellationToken);
                await blobClient.DownloadContentAsync(cancellationToken);

                await blobClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, new BlobRequestConditions(), cancellationToken);

                return HealthCheckResult.Healthy("Successfully connected.");
            }
        }
        catch (RequestFailedException rfe) when (rfe.IsConnectedStoreCustomerError())
        {
            return GetConnectedStoreDegradedResult(rfe);
        }
        catch (Exception ex) when (ex.IsStorageAccountUnknownError())
        {
            return GetConnectedStoreDegradedResult(ex);
        }
        finally
        {
            // Just in case the blob was successfully created but failed to be deleted, try clean up again
            await TryDeleteBlob(blobClient, cancellationToken);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Do not fail during final clean up")]
    private static async Task TryDeleteBlob(BlobClient blobClient, CancellationToken cancellationToken)
    {
        try
        {
            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, new BlobRequestConditions(), cancellationToken);
        }
        catch (Exception)
        {
            // discard exception. 
        }
    }

    private HealthCheckResult GetConnectedStoreDegradedResult(Exception exception)
    {
        // TODO: Update HealthStatusReason
        return new HealthCheckResult(
            HealthStatus.Degraded,
            _degradedDescription,
            exception,
            new Dictionary<string, object> { { "Reason", HealthStatusReason.CustomerManagedKeyAccessLost.ToString() } });
    }
}
