// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using NSubstitute;
using Xunit;
using Azure.Storage.Blobs;
using System.IO;
using System.Threading;
using Azure.Storage.Blobs.Models;
using Microsoft.Health.Dicom.Core.Exceptions;
using Azure.Storage.Blobs.Specialized;
using NSubstitute.ExceptionExtensions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Health.Dicom.Core;
using Azure;

namespace Microsoft.Health.Dicom.Blob.UnitTests;

public class BlobFileStoreTests
{
    [Fact]
    public async Task GivenExternalStore_WhenUploadFails_ThenThrowExceptionWithRightMessageAndProperty()
    {
        InitializeExternalBlobFileStore(out BlobFileStore blobFileStore, out TestExternalBlobClient client);
        client.BlockBlobClient.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>()).Throws(new System.Exception());

        var ex = await Assert.ThrowsAsync<DataStoreException>(() => blobFileStore.StoreFileAsync(1, Substitute.For<Stream>(), CancellationToken.None));

        Assert.True(ex.IsExternal);
        Assert.Equal(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ExternalDataStoreOperationFailed, new System.Exception().Message), ex.Message);
    }

    [Fact]
    public async Task GivenInternalStore_WhenUploadFails_ThenThrowExceptionWithRightMessageAndProperty()
    {
        InitializeInternalBlobFileStore(out BlobFileStore blobFileStore, out TestInternalBlobClient client);
        client.BlockBlobClient.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>()).Throws(new System.Exception());

        var ex = await Assert.ThrowsAsync<DataStoreException>(() => blobFileStore.StoreFileAsync(1, Substitute.For<Stream>(), CancellationToken.None));

        Assert.False(ex.IsExternal);
        Assert.Equal(DicomCoreResource.DataStoreOperationFailed, ex.Message);
    }

    [Fact]
    public async Task GivenExternalStore_WhenGetFails_ThenThrowExceptionWithRightMessageAndProperty()
    {
        InitializeExternalBlobFileStore(out BlobFileStore blobFileStore, out TestExternalBlobClient client);
        RequestFailedException requestFailedException = new RequestFailedException(status: 404, message: "test", errorCode: BlobErrorCode.BlobNotFound.ToString(), innerException: null);
        client.BlockBlobClient.DownloadStreamingAsync(Arg.Any<HttpRange>(), Arg.Any<BlobRequestConditions>(), false, Arg.Any<CancellationToken>()).Throws(requestFailedException);

        var ex = await Assert.ThrowsAsync<ItemNotFoundException>(() => blobFileStore.GetStreamingFileAsync(1, CancellationToken.None));

        Assert.True(ex.IsExternal);
        Assert.Equal(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ExternalDataStoreOperationFailed, "test"), ex.Message);
    }

    [Fact]
    public async Task GivenInternalStore_WhenGetPropertiesFails_ThenThrowExceptionWithRightMessageAndProperty()
    {
        InitializeInternalBlobFileStore(out BlobFileStore blobFileStore, out TestInternalBlobClient client);
        client.BlockBlobClient.GetPropertiesAsync(Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>()).Throws(new System.Exception());

        var ex = await Assert.ThrowsAsync<DataStoreException>(() => blobFileStore.GetFilePropertiesAsync(1, CancellationToken.None));

        Assert.False(ex.IsExternal);
        Assert.Equal(DicomCoreResource.DataStoreOperationFailed, ex.Message);
    }

    private static void InitializeInternalBlobFileStore(out BlobFileStore blobFileStore, out TestInternalBlobClient externalBlobClient)
    {
        externalBlobClient = new TestInternalBlobClient();
        var options = Substitute.For<IOptions<BlobOperationOptions>>();
        options.Value.Returns(Substitute.For<BlobOperationOptions>());
        blobFileStore = new BlobFileStore(externalBlobClient, Substitute.For<DicomFileNameWithPrefix>(), options, NullLogger<BlobFileStore>.Instance);

    }

    private static void InitializeExternalBlobFileStore(out BlobFileStore blobFileStore, out TestExternalBlobClient externalBlobClient)
    {
        externalBlobClient = new TestExternalBlobClient();
        var options = Substitute.For<IOptions<BlobOperationOptions>>();
        options.Value.Returns(Substitute.For<BlobOperationOptions>());
        blobFileStore = new BlobFileStore(externalBlobClient, Substitute.For<DicomFileNameWithPrefix>(), options, NullLogger<BlobFileStore>.Instance);

    }

    private class TestExternalBlobClient : IBlobClient
    {
        public TestExternalBlobClient()
        {
            BlobContainerClient = Substitute.For<BlobContainerClient>();
            BlockBlobClient = Substitute.For<BlockBlobClient>();
            BlobContainerClient.GetBlockBlobClient(Arg.Any<string>()).Returns(BlockBlobClient);
        }

        public virtual BlobContainerClient BlobContainerClient { get; private set; }

        public bool IsExternal => true;

        public BlockBlobClient BlockBlobClient { get; private set; }
    }

    private class TestInternalBlobClient : IBlobClient
    {
        public TestInternalBlobClient()
        {
            BlobContainerClient = Substitute.For<BlobContainerClient>();
            BlockBlobClient = Substitute.For<BlockBlobClient>();
            BlobContainerClient.GetBlockBlobClient(Arg.Any<string>()).Returns(BlockBlobClient);
        }

        public virtual BlobContainerClient BlobContainerClient { get; private set; }

        public bool IsExternal => false;

        public BlockBlobClient BlockBlobClient { get; private set; }
    }

}
