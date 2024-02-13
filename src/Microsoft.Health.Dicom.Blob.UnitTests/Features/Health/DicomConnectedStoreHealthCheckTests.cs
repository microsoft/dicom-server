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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Dicom.Blob.Features.Health;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Utilities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.Health;

public class DicomConnectedStoreHealthCheckTests
{
    private readonly BlobContainerClient _blobContainerClient = Substitute.For<BlobContainerClient>();
    private readonly BlockBlobClient _blockBlobClient = Substitute.For<BlockBlobClient>();
    private readonly IBlobClient _blobClient = Substitute.For<IBlobClient>();
    private readonly IOptions<ExternalBlobDataStoreConfiguration> _externalBlobOptions = Substitute.For<IOptions<ExternalBlobDataStoreConfiguration>>();

    private readonly DicomConnectedStoreHealthCheck _dicomConnectedStoreHealthCheck;

    public DicomConnectedStoreHealthCheckTests()
    {
        _blobClient.BlobContainerClient.Returns(_blobContainerClient);
        _blobContainerClient.GetBlockBlobClient(Arg.Any<string>()).Returns(_blockBlobClient);

        _externalBlobOptions.Value.Returns(new ExternalBlobDataStoreConfiguration()
        {
            StorageDirectory = "AHDS/",
        });

        var logger = new NullLogger<DicomConnectedStoreHealthCheck>();

        _dicomConnectedStoreHealthCheck = new DicomConnectedStoreHealthCheck(_blobClient, _externalBlobOptions, logger);
    }

    [Fact]
    public async Task GivenHealthyConnection_RunHealthCheck_ReturnsHealthy()
    {
        HealthCheckResult result = await _dicomConnectedStoreHealthCheck.CheckHealthAsync(null, CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Theory]
    [InlineData(400, "UnsupportedHeader")]
    [InlineData(401, "InvalidAuthenticationInfo")]
    [InlineData(403, "InvalidAuthenticationInfo")]
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
    public async Task GivenRequestFailedExceptionForCustomerError_RunHealthCheck_ReturnsDegraded(int statusCode, string blobErrorCode)
    {
        _blockBlobClient.DownloadContentAsync(Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(statusCode, "Error", blobErrorCode, new System.Exception()));

        HealthCheckResult result = await _dicomConnectedStoreHealthCheck.CheckHealthAsync(null, CancellationToken.None);

        result.Data.TryGetValue("Reason", out object healthStatusReason);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal(HealthStatusReason.ConnectedStoreDegraded.ToString(), healthStatusReason.ToString());
    }

    [Theory]
    [InlineData(400, "SomeErrorFromDicomBug")]
    [InlineData(401, "AuthErrorFromDicomBug")]
    [InlineData(403, "AuthErrorFromDicomBug")]
    [InlineData(404, "NotFoundDueToDicomBug")]
    [InlineData(409, "ConflictDueToDicomBug")]
    public async Task GivenRequestFailedExceptionForServiceError_RunHealthCheck_ThrowsException(int statusCode, string blobErrorCode)
    {
        _blockBlobClient.DownloadContentAsync(Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(statusCode, "Error", blobErrorCode, new System.Exception()));

        await Assert.ThrowsAsync<RequestFailedException>(async () => await _dicomConnectedStoreHealthCheck.CheckHealthAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task HostNotFound_RunHealthCheck_ReturnsDegraded()
    {
        _blockBlobClient.UploadAsync(Arg.Any<Stream>(), cancellationToken: Arg.Any<CancellationToken>())
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
    public async Task NameOrServiceNotKnown_RunHealthCheck_ReturnsDegraded()
    {
        _blockBlobClient.UploadAsync(Arg.Any<Stream>(), cancellationToken: Arg.Any<CancellationToken>())
            .Throws(new AggregateException(new List<Exception>()
            {
                new Exception("Name or service not known."),
                new Exception("Name or service not known."),
                new Exception("Name or service not known."),
            }));

        HealthCheckResult result = await _dicomConnectedStoreHealthCheck.CheckHealthAsync(null, CancellationToken.None);

        result.Data.TryGetValue("Reason", out object healthStatusReason);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal(HealthStatusReason.ConnectedStoreDegraded.ToString(), healthStatusReason.ToString());
    }

    [Fact]
    public async Task DeleteFails_RunHealthCheck_ReturnsDegradedAndExceptionIsNotThrown()
    {
        _blockBlobClient.DeleteAsync(Arg.Any<DeleteSnapshotsOption>(), Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(403, "Failure", BlobErrorCode.AuthorizationFailure.ToString(), new System.Exception()));

        HealthCheckResult result = await _dicomConnectedStoreHealthCheck.CheckHealthAsync(null, CancellationToken.None);

        result.Data.TryGetValue("Reason", out object healthStatusReason);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal(HealthStatusReason.ConnectedStoreDegraded.ToString(), healthStatusReason.ToString());
    }
}
