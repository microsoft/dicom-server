// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Features.ExternalStore;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.Storage;

public class BlobFileStoreTests
{
    private const string DefaultBlobName = "foo/123.dcm";
    private const string DefaultStorageDirectory = "/test/";

    [Theory]
    [InlineData("a!/b")]
    [InlineData("a#/b")]
    [InlineData("a\b")]
    [InlineData("a%b")]
    public void GivenInvalidStorageDirectory_WhenExternalStoreInitialized_ThenThrowExceptionWithRightMessageAndProperty(string storageDirectory)
    {
        ExternalBlobDataStoreConfiguration config = new ExternalBlobDataStoreConfiguration() { StorageDirectory = storageDirectory };
        var results = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(config, new ValidationContext(config), results, validateAllProperties: true));

        Assert.Single(results);
        Assert.Equal("""The field StorageDirectory must match the regular expression '^[a-zA-Z0-9\-\.]*(\/[a-zA-Z0-9\-\.]*){0,254}$'.""", results.First().ErrorMessage);
    }

    [Theory]
    [InlineData("//")]
    [InlineData("a/")]
    [InlineData("a")]
    [InlineData("a/b/")]
    [InlineData("a-b/c-d/")]
    public void GivenValidStorageDirectory_WhenExternalStoreInitialized_ThenDoNotThrowException(string storageDirectory)
    {
        ExternalBlobDataStoreConfiguration config = new ExternalBlobDataStoreConfiguration() { StorageDirectory = storageDirectory };
        var results = new List<ValidationResult>();

        Assert.True(Validator.TryValidateObject(config, new ValidationContext(config), results, validateAllProperties: true));
    }

    [Fact]
    public void GivenInvalidStorageDirectorySegments_WhenExternalStoreInitialized_ThenThrowExceptionWithRightMessageAndProperty()
    {
        ExternalBlobDataStoreConfiguration config = new ExternalBlobDataStoreConfiguration() { StorageDirectory = string.Join("", Enumerable.Repeat("a/b", 255)) };
        var results = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(config, new ValidationContext(config), results, validateAllProperties: true));

        Assert.Single(results);
        Assert.Equal("""The field StorageDirectory must match the regular expression '^[a-zA-Z0-9\-\.]*(\/[a-zA-Z0-9\-\.]*){0,254}$'.""", results.First().ErrorMessage);
    }

    [Fact]
    public void GivenInvalidStorageDirectoryLength_WhenExternalStoreInitialized_ThenThrowExceptionWithRightMessageAndProperty()
    {
        ExternalBlobDataStoreConfiguration config = new ExternalBlobDataStoreConfiguration() { StorageDirectory = string.Join("", Enumerable.Repeat("a", 1025)) };
        var results = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(config, new ValidationContext(config), results, validateAllProperties: true));

        Assert.Single(results);
        Assert.Equal("""The field StorageDirectory must be a string with a maximum length of 1024.""", results.First().ErrorMessage);
    }

    [Fact]
    public async Task GivenExternalStore_WhenUploadFails_ThenThrowExceptionWithRightMessageAndProperty()
    {
        InitializeExternalBlobFileStore(out BlobFileStore blobFileStore, out ExternalBlobClient client);
        client.BlobContainerClient.GetBlockBlobClient(DefaultBlobName).UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>()).Throws
        (new System.Exception());

        var ex = await Assert.ThrowsAsync<DataStoreException>(() => blobFileStore.StoreFileAsync(1, Partition.DefaultName, Substitute.For<Stream>(), CancellationToken.None));

        Assert.True(ex.IsExternal);
        Assert.Equal(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ExternalDataStoreOperationFailed, new System.Exception().Message), ex.Message);
    }

    [Fact]
    public async Task GivenInternalStore_WhenUploadFails_ThenThrowExceptionWithRightMessageAndProperty()
    {
        InitializeInternalBlobFileStore(out BlobFileStore blobFileStore, out TestInternalBlobClient client);
        client.BlockBlobClient.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>()).Throws(new System.Exception());

        var ex = await Assert.ThrowsAsync<DataStoreException>(() => blobFileStore.StoreFileAsync(1, Partition.DefaultName, Substitute.For<Stream>(), CancellationToken.None));

        Assert.False(ex.IsExternal);
        Assert.Equal(DicomCoreResource.DataStoreOperationFailed, ex.Message);
    }

    [Fact]
    public async Task GivenExternalStore_WhenGetFailsBecauseBlobNotFound_ThenThrowExceptionWithRightMessageAndProperty()
    {
        InitializeExternalBlobFileStore(out BlobFileStore blobFileStore, out ExternalBlobClient client);
        RequestFailedException requestFailedException = new RequestFailedException(status: 404, message: "test", errorCode: BlobErrorCode.BlobNotFound.ToString(), innerException: null);
        client.BlobContainerClient.GetBlockBlobClient(DefaultBlobName).DownloadStreamingAsync(
            Arg.Any<HttpRange>(),
            Arg.Any<BlobRequestConditions>(),
            false,
            Arg.Any<CancellationToken>()).Throws(requestFailedException);

        var ex = await Assert.ThrowsAsync<DataStoreRequestFailedException>(() => blobFileStore.GetStreamingFileAsync(1, Partition.DefaultName, CancellationToken.None));

        Assert.True(ex.IsExternal);
        Assert.Equal(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ExternalDataStoreOperationFailed, BlobErrorCode.BlobNotFound.ToString()), ex.Message);
    }

    [Fact]
    public async Task GivenExternalStore_WhenRequestFails_ThenThrowExceptionWithRightMessageAndProperty()
    {
        InitializeExternalBlobFileStore(out BlobFileStore blobFileStore, out ExternalBlobClient client);
        RequestFailedException requestFailedAuthException = new RequestFailedException(
            status: 400,
            message: "auth failed simulation",
            errorCode: BlobErrorCode.AuthenticationFailed.ToString(),
            innerException: new Exception("super secret inner info"));
        client.BlobContainerClient.GetBlockBlobClient(DefaultBlobName).DownloadStreamingAsync(
            Arg.Any<HttpRange>(),
            Arg.Any<BlobRequestConditions>(),
            false,
            Arg.Any<CancellationToken>()).Throws(requestFailedAuthException);

        var ex = await Assert.ThrowsAsync<DataStoreRequestFailedException>(() => blobFileStore.GetStreamingFileAsync(1, Partition.DefaultName, CancellationToken.None));

        Assert.True(ex.IsExternal);
        Assert.Equal(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ExternalDataStoreOperationFailed, BlobErrorCode.AuthenticationFailed.ToString()), ex.Message);
    }

    [Fact]
    public void GivenExternalStore_WhenGetServiceStorePathWithPartitionsEnabled_ThenPathReturnedContainsPartitionPassed()
    {
        string partitionName = "foo";
        InitializeExternalBlobFileStore(out BlobFileStore _, out ExternalBlobClient client, partitionsEnabled: true);
        Assert.Equal(DefaultStorageDirectory + partitionName + "/", client.GetServiceStorePath(partitionName));
    }

    [Fact]
    public void GivenExternalStore_WhenGetServiceStorePathWithPartitionsDisabled_ThenPathReturnedDoesNotUsePartition()
    {
        InitializeExternalBlobFileStore(out BlobFileStore _, out ExternalBlobClient client, partitionsEnabled: false);
        Assert.Equal(DefaultStorageDirectory, client.GetServiceStorePath("foo"));
    }

    [Fact]
    public async Task GivenInternalStore_WhenGetPropertiesFails_ThenThrowExceptionWithRightMessageAndProperty()
    {
        InitializeInternalBlobFileStore(out BlobFileStore blobFileStore, out TestInternalBlobClient client);
        client.BlockBlobClient.GetPropertiesAsync(Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>()).Throws(new System.Exception());

        var ex = await Assert.ThrowsAsync<DataStoreException>(() => blobFileStore.GetFilePropertiesAsync(1, Partition.DefaultName, CancellationToken.None));

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

    private static void InitializeExternalBlobFileStore(out BlobFileStore blobFileStore, out ExternalBlobClient externalBlobClient, bool partitionsEnabled = false)
    {
        var featureConfiguration = Substitute.For<IOptions<FeatureConfiguration>>();
        featureConfiguration.Value.Returns(new FeatureConfiguration
        {
            EnableDataPartitions = partitionsEnabled,
        });
        var externalStoreConfig = Substitute.For<IOptions<ExternalBlobDataStoreConfiguration>>();
        externalStoreConfig.Value.Returns(new ExternalBlobDataStoreConfiguration
        {
            ConnectionString = "test",
            ContainerName = "test",
            StorageDirectory = DefaultStorageDirectory,
        });
        var clientOptions = Substitute.For<IOptions<BlobServiceClientOptions>>();
        clientOptions.Value.Returns(Substitute.For<BlobServiceClientOptions>());
        externalBlobClient = new ExternalBlobClient(
            Substitute.For<IExternalOperationCredentialProvider>(),
            externalStoreConfig,
            clientOptions,
            featureConfiguration);

        var blobContainerClient = Substitute.For<BlobContainerClient>();
        blobContainerClient.GetBlockBlobClient(Arg.Any<string>()).Returns(Substitute.For<BlockBlobClient>());
        externalBlobClient.BlobContainerClient = blobContainerClient;

        var options = Substitute.For<IOptions<BlobOperationOptions>>();
        options.Value.Returns(Substitute.For<BlobOperationOptions>());
        blobFileStore = new BlobFileStore(externalBlobClient, Substitute.For<DicomFileNameWithPrefix>(), options, NullLogger<BlobFileStore>.Instance);
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

        public string GetServiceStorePath(string _)
        {
            return "internal/";
        }
    }

}
