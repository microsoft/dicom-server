// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Health.Dicom.Blob.Features.ExternalStore;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Tests.Common.Telemetry;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.Storage;

public class BlobFileStoreDiagnosticTests : BlobFileStoreTests
{
    [Fact]
    public async Task GivenExternalStore_WhenOperationFailsWithRequestFailedException_ThenDiagnosticLogEmitted()
    {
        InitializeExternalBlobFileStore(out BlobFileStore blobFileStore, out ExternalBlobClient client, out MockTelemetryChannel channel);

        RequestFailedException requestFailedException = new RequestFailedException(
            status: 412,
            message: "Condition was not met.",
            errorCode: BlobErrorCode.ConditionNotMet.ToString(),
            innerException: new Exception("Condition not met."));

        client.BlobContainerClient.GetBlockBlobClient(DefaultBlobName).GetPropertiesAsync(
            Arg.Any<BlobRequestConditions>(),
            Arg.Any<CancellationToken>()).Throws(requestFailedException);

        await Assert.ThrowsAsync<DataStoreRequestFailedException>(() => blobFileStore.GetFilePropertiesAsync(1, Partition.Default, _defaultFileProperties, CancellationToken.None));

        AssertDiagnosticTelemetryEmitted(channel, requestFailedException.ErrorCode!);
    }

    [Fact]
    public async Task GivenInternalStore_WhenOperationFailsWithRequestFailedException_ThenDiagnosticLogEmitted()
    {
        InitializeInternalBlobFileStore(out BlobFileStore blobFileStore, out TestInternalBlobClient client, out MockTelemetryChannel channel);

        RequestFailedException requestFailedException = new RequestFailedException(
            status: 412,
            message: "Condition was not met.",
            errorCode: BlobErrorCode.ConditionNotMet.ToString(),
            innerException: new Exception("Condition not met."));

        client.BlobContainerClient.GetBlockBlobClient(DefaultBlobName).GetPropertiesAsync(
            Arg.Any<BlobRequestConditions>(),
            Arg.Any<CancellationToken>()).Throws(requestFailedException);

        await Assert.ThrowsAsync<DataStoreRequestFailedException>(() => blobFileStore.GetFilePropertiesAsync(1, Partition.Default, _defaultFileProperties, CancellationToken.None));

        AssertDiagnosticTelemetryEmitted(channel, requestFailedException.ErrorCode!);
    }

    [Fact]
    public async Task GivenExternalStore_WhenOperationFailsWithInternalException_ThenDiagnosticLogEmitted()
    {
        InitializeExternalBlobFileStore(out BlobFileStore blobFileStore, out ExternalBlobClient client, out MockTelemetryChannel channel);

        client.BlobContainerClient.GetBlockBlobClient(DefaultBlobName).GetPropertiesAsync(
            Arg.Any<BlobRequestConditions>(),
            Arg.Any<CancellationToken>()).Throws(new Exception("unknown"));

        await Assert.ThrowsAsync<DataStoreException>(() =>
            blobFileStore.GetFilePropertiesAsync(1, Partition.Default, _defaultFileProperties, CancellationToken.None));

        AssertDiagnosticTelemetryEmitted(channel, DicomCoreResource.ExternalDataStoreOperationFailedUnknownIssue);
    }


    [Fact]
    public async Task GivenInternalStore_WhenOperationFailsWithInternalException_ThenDiagnosticLogEmitted()
    {
        InitializeInternalBlobFileStore(out BlobFileStore blobFileStore, out TestInternalBlobClient client, out MockTelemetryChannel channel);

        client.BlobContainerClient.GetBlockBlobClient(DefaultBlobName).GetPropertiesAsync(
            Arg.Any<BlobRequestConditions>(),
            Arg.Any<CancellationToken>()).Throws(new Exception("unknown"));

        await Assert.ThrowsAsync<DataStoreException>(() =>
            blobFileStore.GetFilePropertiesAsync(1, Partition.Default, _defaultFileProperties, CancellationToken.None));

        AssertDiagnosticTelemetryEmitted(channel, DicomCoreResource.ExternalDataStoreOperationFailedUnknownIssue);
    }

    [Fact]
    public async Task GivenExternalStore_WhenOperationSucceeds_ThenDiagnosticLogEmitted()
    {
        InitializeExternalBlobFileStore(out BlobFileStore blobFileStore, out ExternalBlobClient client, out MockTelemetryChannel channel);

        var expectedResult = Substitute.For<Response<bool>>();
        expectedResult.Value.Returns(true);

        client.BlobContainerClient.GetBlockBlobClient(DefaultBlobName).DeleteIfExistsAsync(
            Arg.Any<DeleteSnapshotsOption>(),
            conditions: Arg.Any<BlobRequestConditions>(),
            Arg.Any<CancellationToken>()).Returns(expectedResult);

        await blobFileStore.DeleteFileIfExistsAsync(1, Partition.Default, _defaultFileProperties, CancellationToken.None);

        AssertDiagnosticTelemetryEmitted(channel, DicomCoreResource.ExternalDataStoreOperationSucceeded);
    }

    [Fact]
    public async Task GivenInternalStore_WhenOperationSucceeds_ThenDiagnosticLogEmitted()
    {
        InitializeInternalBlobFileStore(out BlobFileStore blobFileStore, out TestInternalBlobClient client, out MockTelemetryChannel channel);

        var expectedResult = Substitute.For<Response<bool>>();
        expectedResult.Value.Returns(true);

        client.BlobContainerClient.GetBlockBlobClient(DefaultBlobName).DeleteIfExistsAsync(
            Arg.Any<DeleteSnapshotsOption>(),
            conditions: Arg.Any<BlobRequestConditions>(),
            Arg.Any<CancellationToken>()).Returns(expectedResult);

        await blobFileStore.DeleteFileIfExistsAsync(1, Partition.Default, _defaultFileProperties, CancellationToken.None);

        AssertDiagnosticTelemetryEmitted(channel, DicomCoreResource.ExternalDataStoreOperationSucceeded);
    }

    private void AssertDiagnosticTelemetryEmitted(MockTelemetryChannel channel, string expectedMessage)
    {
        Assert.Single(channel.Items);
        var firstItem = channel.Items[0];
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Contains(expectedMessage,
            ((Microsoft.ApplicationInsights.DataContracts.TraceTelemetry)firstItem).Message);
        Assert.Equal(3, firstItem.Context.Properties.Count);
        Assert.Equal(Boolean.TrueString, firstItem.Context.Properties["forwardLog"]);
        Assert.Equal(_defaultFileProperties.ETag,
            firstItem.Context.Properties["dicomAdditionalInformation_filePropertiesETag"]);
        Assert.Equal(_defaultFileProperties.Path,
            firstItem.Context.Properties["dicomAdditionalInformation_filePropertiesPath"]);
#pragma warning restore CS0618 // Type or member is obsolete
    }
}