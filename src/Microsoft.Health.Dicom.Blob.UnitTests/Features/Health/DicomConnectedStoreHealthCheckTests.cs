// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Dicom.Blob.Features.Health;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.Health;

public class DicomConnectedStoreHealthCheckTests
{
    private readonly BlobContainerClient _blobContainerClient = Substitute.For<BlobContainerClient>();
    private readonly BlockBlobClient _blockBlobClient = Substitute.For<BlockBlobClient>();
    private readonly IBlobClient _blobClient = Substitute.For<IBlobClient>();

    private readonly DicomConnectedStoreHealthCheck _dicomConnectedStoreHealthCheck;

    public DicomConnectedStoreHealthCheckTests()
    {
        _blobClient.BlobContainerClient.Returns(_blobContainerClient);
        _blobContainerClient.GetBlockBlobClient(Arg.Any<string>()).Returns(_blockBlobClient);

        _dicomConnectedStoreHealthCheck = new DicomConnectedStoreHealthCheck(_blobClient);
    }

    [Fact]
    public async Task GivenHealthyConnection_RunHealthCheck_ReturnsHealthy()
    {
        HealthCheckResult result = await _dicomConnectedStoreHealthCheck.CheckHealthAsync(null, CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Theory]
    [InlineData(403, "AuthorizationFailure")]
    [InlineData(403, "AuthorizationPermissionMismatch")]
    [InlineData(403, "InsufficientAccountPermissions")]
    [InlineData(403, "AccountIsDisabled")]
    [InlineData(403, "KeyVaultEncryptionKeyNotFound")]
    [InlineData(403, "KeyVaultAccessTokenCannotBeAcquired")]
    [InlineData(403, "KeyVaultVaultNotFound")]
    [InlineData(404, "ContainerNotFound")]
    [InlineData(404, "FilesystemNotFound")]
    [InlineData(409, "ContainerBeingDeleted")]
    [InlineData(409, "ContainerDisabled")]
    public async Task GivenRequestFailedExceptionForCustomerError_RunHealthCheck_ReturnsDegradedAndDeletesBlob(int statusCode, string blobErrorCode)
    {
        _blockBlobClient.DownloadContentAsync(Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(statusCode, "Error", blobErrorCode, new System.Exception()));

        HealthCheckResult result = await _dicomConnectedStoreHealthCheck.CheckHealthAsync(null, CancellationToken.None);

        result.Data.TryGetValue("Reason", out object healthStatusReason);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal(HealthStatusReason.ConnectedStoreDegraded.ToString(), healthStatusReason.ToString());

        await _blockBlobClient.Received(2).DeleteIfExistsAsync(Arg.Any<DeleteSnapshotsOption>(), Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(400, "SomeErrorFromDicomBug")]
    [InlineData(403, "AuthErrorFromDicomBug")]
    [InlineData(404, "NotFoundDueToDicomBug")]
    [InlineData(409, "ConflictDueToDicomBug")]
    public async Task GivenRequestFailedExceptionForServiceError_RunHealthCheck_ThrowsExceptionAndCleansUpBlob(int statusCode, string blobErrorCode)
    {
        _blockBlobClient.DownloadContentAsync(Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(statusCode, "Error", blobErrorCode, new System.Exception()));

        await Assert.ThrowsAsync<RequestFailedException>(async () => await _dicomConnectedStoreHealthCheck.CheckHealthAsync(null, CancellationToken.None));

        await _blockBlobClient.Received(2).DeleteIfExistsAsync(Arg.Any<DeleteSnapshotsOption>(), Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HostNotFound_RunHealthCheck_ReturnsDegraded()
    {
        _blockBlobClient.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>())
            .Throws(new AggregateException(new List<Exception>()
            {
                new Exception("No such host is known."),
                new Exception("No such host is known."),
                new Exception("No such host is known."),
            }));

        HealthCheckResult result = await _dicomConnectedStoreHealthCheck.CheckHealthAsync(null, CancellationToken.None);

        result.Data.TryGetValue("Reason", out object healthStatusReason);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal(HealthStatusReason.ConnectedStoreDegraded.ToString(), healthStatusReason.ToString());
    }

    [Fact]
    public async Task AllDeleteFails_RunHealthCheck_ReturnsDegradedAndExceptionIsNotThrown()
    {
        _blockBlobClient.DeleteIfExistsAsync(Arg.Any<DeleteSnapshotsOption>(), Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(403, "Failure", BlobErrorCode.AuthorizationFailure.ToString(), new System.Exception()));

        _blockBlobClient.DeleteAsync(Arg.Any<DeleteSnapshotsOption>(), Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(403, "Failure", BlobErrorCode.AuthorizationFailure.ToString(), new System.Exception()));

        HealthCheckResult result = await _dicomConnectedStoreHealthCheck.CheckHealthAsync(null, CancellationToken.None);

        result.Data.TryGetValue("Reason", out object healthStatusReason);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal(HealthStatusReason.ConnectedStoreDegraded.ToString(), healthStatusReason.ToString());
    }

    [Fact]
    public async Task DeleteBeforeAndAfterFails_RunHealthCheck_ReturnsHealthyAndExceptionIsNotThrown()
    {
        // the extra deletes before and after the checks fail, but everything else successfully finishes
        _blockBlobClient.DeleteIfExistsAsync(Arg.Any<DeleteSnapshotsOption>(), Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(403, "Failure", BlobErrorCode.AuthorizationFailure.ToString(), new System.Exception()));

        HealthCheckResult result = await _dicomConnectedStoreHealthCheck.CheckHealthAsync(null, CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }
}
