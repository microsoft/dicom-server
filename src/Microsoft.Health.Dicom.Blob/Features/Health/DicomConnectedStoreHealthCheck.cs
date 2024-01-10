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
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Dicom.Blob.Extensions;
using Microsoft.Health.Dicom.Blob.Features.Storage;

namespace Microsoft.Health.Dicom.Blob.Features.Health;

internal class DicomConnectedStoreHealthCheck : IHealthCheck
{
    private readonly string _degradedDescription = "The health of the connected store has degraded.";

    private readonly string _testContent = "Test content.";
    private readonly string _testDirectoryName = "DicomServiceHealthCheck";
    private readonly string _testFileName = "healthCheck.txt";

    private readonly IBlobClient _blobClient;

    public DicomConnectedStoreHealthCheck(IBlobClient blobClient)
    {
        _blobClient = EnsureArg.IsNotNull(blobClient, nameof(blobClient));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = _blobClient.BlobContainerClient;
        BlockBlobClient blobClient = containerClient.GetBlockBlobClient($"{_testDirectoryName}/{_testFileName}");

        // start trying to delete the blob, in case delete failed on the previous run
        await TryDeleteBlob(blobClient, cancellationToken);

        try
        {
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(_testContent)))
            {
                string blockId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

                // test blob/write and that block blobs are supported
                await blobClient.StageBlockAsync(blockId, stream, cancellationToken: cancellationToken);
                await blobClient.CommitBlockListAsync(new List<string> { blockId }, new CommitBlockListOptions(), cancellationToken);

                // test blob/read and blob/metadata/read
                await blobClient.DownloadContentAsync(cancellationToken);

                // test blob/delete
                await blobClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, new BlobRequestConditions(), cancellationToken);

                return HealthCheckResult.Healthy("Successfully connected.");
            }
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
            // Just in case the blob was successfully created but failed to be deleted, try clean up again
            await TryDeleteBlob(blobClient, cancellationToken);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Do not fail during final clean up")]
    private static async Task TryDeleteBlob(BlockBlobClient blobClient, CancellationToken cancellationToken)
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
        return new HealthCheckResult(
            HealthStatus.Degraded,
            _degradedDescription,
            exception,
            new Dictionary<string, object> { { "Reason", HealthStatusReason.ConnectedStoreDegraded.ToString() } });
    }
}
