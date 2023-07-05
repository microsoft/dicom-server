// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Features.Transactions;
using Microsoft.Health.Core.Internal;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Delete;

public class DeleteServiceTests
{
    private readonly DeleteService _deleteService;
    private readonly IIndexDataStore _indexDataStore;
    private readonly IFileStore _fileDataStore;
    private readonly ITransactionScope _transactionScope;
    private readonly DeletedInstanceCleanupConfiguration _deleteConfiguration;
    private readonly IMetadataStore _metadataStore;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;

    public DeleteServiceTests()
    {
        _indexDataStore = Substitute.For<IIndexDataStore>();
        _metadataStore = Substitute.For<IMetadataStore>();
        _fileDataStore = Substitute.For<IFileStore>();
        _deleteConfiguration = new DeletedInstanceCleanupConfiguration
        {
            DeleteDelay = TimeSpan.FromDays(1),
            BatchSize = 10,
            MaxRetries = 5,
            PollingInterval = TimeSpan.FromSeconds(1),
            RetryBackOff = TimeSpan.FromDays(4),
        };

        IOptions<DeletedInstanceCleanupConfiguration> deletedInstanceCleanupConfigurationOptions = Substitute.For<IOptions<DeletedInstanceCleanupConfiguration>>();
        deletedInstanceCleanupConfigurationOptions.Value.Returns(_deleteConfiguration);
        ITransactionHandler transactionHandler = Substitute.For<ITransactionHandler>();
        _transactionScope = Substitute.For<ITransactionScope>();
        transactionHandler.BeginTransaction().Returns(_transactionScope);
        _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        _dicomRequestContextAccessor.RequestContext.DataPartitionEntry = PartitionEntry.Default;

        _deleteService = new DeleteService(_indexDataStore, _metadataStore, _fileDataStore, deletedInstanceCleanupConfigurationOptions, transactionHandler, NullLogger<DeleteService>.Instance, _dicomRequestContextAccessor);
    }

    [Fact]
    public async Task GivenADeleteStudyRequest_WhenDataStoreIsCalled_ThenCorrectDeleteDelayIsUsed()
    {
        string studyInstanceUid = TestUidGenerator.Generate();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        using (Mock.Property(() => ClockResolver.UtcNowFunc, () => now))
        {
            await _deleteService.DeleteStudyAsync(studyInstanceUid, CancellationToken.None);
            await _indexDataStore
                .Received(1)
                .DeleteStudyIndexAsync(DefaultPartition.Key, studyInstanceUid, now + _deleteConfiguration.DeleteDelay);
        }
    }

    [Fact]
    public async Task GivenADeleteSeriesRequest_WhenDataStoreIsCalled_ThenCorrectDeleteDelayIsUsed()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        using (Mock.Property(() => ClockResolver.UtcNowFunc, () => now))
        {
            await _deleteService.DeleteSeriesAsync(studyInstanceUid, seriesInstanceUid, CancellationToken.None);
            await _indexDataStore
                .Received(1)
                .DeleteSeriesIndexAsync(DefaultPartition.Key, studyInstanceUid, seriesInstanceUid, now + _deleteConfiguration.DeleteDelay);
        }
    }

    [Fact]
    public async Task GivenADeleteInstanceRequest_WhenDataStoreIsCalled_ThenCorrectDeleteDelayIsUsed()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        using (Mock.Property(() => ClockResolver.UtcNowFunc, () => now))
        {
            await _deleteService.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, CancellationToken.None);
            await _indexDataStore
                .Received(1)
                .DeleteInstanceIndexAsync(DefaultPartition.Key, studyInstanceUid, seriesInstanceUid, sopInstanceUid, now + _deleteConfiguration.DeleteDelay);
        }
    }

    [Fact]
    public async Task GivenADeleteInstanceRequestWithNonDefaultPartition_WhenDataStoreIsCalled_ThenNonDefaultPartitionIsUsed()
    {
        List<InstanceMetadata> responseList = GeneratedDeletedInstanceList(1, partitionEntry: new PartitionEntry(123, "ANonDefaultName"));

        _indexDataStore
            .RetrieveDeletedInstancesWithPropertiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(responseList);

        (bool success, int retrievedInstanceCount) = await _deleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

        await ValidateSuccessfulCleanupDeletedInstanceCall(success, responseList.Select(x => x.VersionedInstanceIdentifier).ToList(), retrievedInstanceCount);
    }

    [Fact]
    public async Task GivenNoDeletedInstances_WhenCleanupCalled_ThenNotCallStoresAndReturnsCorrectTuple()
    {
        _indexDataStore
            .RetrieveDeletedInstancesWithPropertiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(new List<InstanceMetadata>());

        (bool success, int retrievedInstanceCount) = await _deleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

        Assert.True(success);
        Assert.Equal(0, retrievedInstanceCount);

        await _indexDataStore
            .ReceivedWithAnyArgs(1)
            .RetrieveDeletedInstancesWithPropertiesAsync(batchSize: default, maxRetries: default, CancellationToken.None);

        await _indexDataStore
            .DidNotReceiveWithAnyArgs()
            .DeleteDeletedInstanceAsync(versionedInstanceIdentifier: default, CancellationToken.None);

        await _indexDataStore
            .DidNotReceiveWithAnyArgs()
            .IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier: default, cleanupAfter: default, CancellationToken.None);

        await _fileDataStore
            .DidNotReceiveWithAnyArgs()
            .DeleteFileIfExistsAsync(version: default, DefaultPartition.Name, CancellationToken.None);

        await _metadataStore
            .DidNotReceiveWithAnyArgs()
            .DeleteInstanceMetadataIfExistsAsync(version: default, CancellationToken.None);

        _transactionScope.Received(1).Complete();
    }

    [Fact]
    public async Task GivenADeletedInstance_WhenFileStoreThrows_ThenIncrementRetryIsCalled()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using (Mock.Property(() => ClockResolver.UtcNowFunc, () => now))
        {
            List<InstanceMetadata> responseList = GeneratedDeletedInstanceList(1);

            _indexDataStore
                .RetrieveDeletedInstancesWithPropertiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(responseList);

            _fileDataStore
                .DeleteFileIfExistsAsync(Arg.Any<long>(), DefaultPartition.Name, Arg.Any<CancellationToken>())
                .ThrowsForAnyArgs(new Exception("Generic exception"));

            (bool success, int retrievedInstanceCount) = await _deleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

            Assert.True(success);
            Assert.Equal(1, retrievedInstanceCount);

            await _indexDataStore
                .Received(1)
                .IncrementDeletedInstanceRetryAsync(responseList[0].VersionedInstanceIdentifier, now + _deleteConfiguration.RetryBackOff, CancellationToken.None);
        }
    }

    [Fact]
    public async Task GivenADeletedInstance_WhenMetadataStoreThrowsUnhandled_ThenIncrementRetryIsCalled()
    {
        List<InstanceMetadata> responseList = GeneratedDeletedInstanceList(1);

        _indexDataStore
            .RetrieveDeletedInstancesWithPropertiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(responseList);

        _metadataStore
            .DeleteInstanceMetadataIfExistsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(new Exception("Generic exception"));

        (bool success, int retrievedInstanceCount) = await _deleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

        Assert.True(success);
        Assert.Equal(1, retrievedInstanceCount);

        await _indexDataStore
            .Received(1)
            .IncrementDeletedInstanceRetryAsync(responseList[0].VersionedInstanceIdentifier, cleanupAfter: Arg.Any<DateTimeOffset>(), CancellationToken.None);
    }

    [Fact]
    public async Task GivenADeletedInstance_WhenIncrementThrows_ThenSuccessIsReturnedFalse()
    {
        List<InstanceMetadata> responseList = GeneratedDeletedInstanceList(1);

        _indexDataStore
            .RetrieveDeletedInstancesWithPropertiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(responseList);

        _fileDataStore
            .DeleteFileIfExistsAsync(Arg.Any<long>(), DefaultPartition.Name, Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(new Exception("Generic exception"));

        _indexDataStore
            .IncrementDeletedInstanceRetryAsync(Arg.Any<VersionedInstanceIdentifier>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(new Exception("Generic exception"));

        (bool success, int retrievedInstanceCount) = await _deleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

        Assert.False(success);
        Assert.Equal(1, retrievedInstanceCount);

        await _indexDataStore
            .Received(1)
            .IncrementDeletedInstanceRetryAsync(responseList[0].VersionedInstanceIdentifier, cleanupAfter: Arg.Any<DateTimeOffset>(), CancellationToken.None);
    }

    [Fact]
    public async Task GivenADeletedInstance_WhenRetrieveThrows_ThenSuccessIsReturnedFalse()
    {
        _indexDataStore
            .RetrieveDeletedInstancesWithPropertiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(new Exception("Generic exception"));

        (bool success, int retrievedInstanceCount) = await _deleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

        Assert.False(success);
        Assert.Equal(0, retrievedInstanceCount);

        await _indexDataStore
            .DidNotReceiveWithAnyArgs()
            .DeleteDeletedInstanceAsync(versionedInstanceIdentifier: default, CancellationToken.None);

        await _indexDataStore
            .DidNotReceiveWithAnyArgs()
            .IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier: default, cleanupAfter: default, CancellationToken.None);

        await _fileDataStore
            .DidNotReceiveWithAnyArgs()
            .DeleteFileIfExistsAsync(version: default, DefaultPartition.Name, CancellationToken.None);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task GivenMultipleDeletedInstance_WhenCleanupCalled_ThenCorrectMethodsAreCalledAndReturnsCorrectTuple(int numberOfDeletedInstances)
    {
        List<InstanceMetadata> responseList = GeneratedDeletedInstanceList(numberOfDeletedInstances);

        _indexDataStore
            .RetrieveDeletedInstancesWithPropertiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(responseList);

        (bool success, int retrievedInstanceCount) = await _deleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

        await ValidateSuccessfulCleanupDeletedInstanceCall(success, responseList.Select(x => x.VersionedInstanceIdentifier).ToList(), retrievedInstanceCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task GivenMultipleDeletedInstanceWithOriginalVersion_WhenCleanupCalled_ThenCorrectMethodsAreCalled(int numberOfDeletedInstances)
    {
        List<InstanceMetadata> responseList = GeneratedDeletedInstanceList(numberOfDeletedInstances, new InstanceProperties { OriginalVersion = 5 });

        _indexDataStore
            .RetrieveDeletedInstancesWithPropertiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(responseList);

        (bool success, int retrievedInstanceCount) = await _deleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

        Assert.True(success);
        Assert.Equal(responseList.Count, retrievedInstanceCount);

        await _indexDataStore
            .ReceivedWithAnyArgs(1)
            .RetrieveDeletedInstancesWithPropertiesAsync(default, default, CancellationToken.None);

        foreach (InstanceMetadata instance in responseList)
        {
            var deletedVersion = instance.VersionedInstanceIdentifier;
            await _indexDataStore
                .Received(1)
                .DeleteDeletedInstanceAsync(deletedVersion, CancellationToken.None);

            await _metadataStore
                .Received(1)
                .DeleteInstanceMetadataIfExistsAsync(deletedVersion.Version, CancellationToken.None);

            await _fileDataStore
                .Received(1)
                .DeleteFileIfExistsAsync(deletedVersion.Version, deletedVersion.PartitionName, CancellationToken.None);

            await _fileDataStore
               .Received(numberOfDeletedInstances)
               .DeleteFileIfExistsAsync(instance.InstanceProperties.OriginalVersion.Value, deletedVersion.PartitionName, CancellationToken.None);

            await _metadataStore
                .Received(numberOfDeletedInstances)
                .DeleteInstanceMetadataIfExistsAsync(instance.InstanceProperties.OriginalVersion.Value, CancellationToken.None);
        }

        await _indexDataStore
            .DidNotReceiveWithAnyArgs()
            .IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier: default, cleanupAfter: default, CancellationToken.None);

        _transactionScope.Received(1).Complete();
    }

    private async Task ValidateSuccessfulCleanupDeletedInstanceCall(bool success, IReadOnlyCollection<VersionedInstanceIdentifier> responseList, int retrievedInstanceCount)
    {
        Assert.True(success);
        Assert.Equal(responseList.Count, retrievedInstanceCount);

        await _indexDataStore
            .ReceivedWithAnyArgs(1)
            .RetrieveDeletedInstancesWithPropertiesAsync(default, default, CancellationToken.None);

        foreach (VersionedInstanceIdentifier deletedVersion in responseList)
        {
            await _indexDataStore
                .Received(1)
                .DeleteDeletedInstanceAsync(deletedVersion, CancellationToken.None);

            await _metadataStore
                .Received(1)
                .DeleteInstanceMetadataIfExistsAsync(deletedVersion.Version, CancellationToken.None);

            await _fileDataStore
                .Received(1)
                .DeleteFileIfExistsAsync(deletedVersion.Version, deletedVersion.PartitionName, CancellationToken.None);
        }

        await _indexDataStore
            .DidNotReceiveWithAnyArgs()
            .IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier: default, cleanupAfter: default, CancellationToken.None);

        _transactionScope.Received(1).Complete();
    }

    private static List<InstanceMetadata> GeneratedDeletedInstanceList(int numberOfResults, InstanceProperties instanceProperties = null, PartitionEntry partitionEntry = null)
    {
        instanceProperties = instanceProperties ?? new InstanceProperties();
        partitionEntry = partitionEntry ?? DefaultPartition.PartitionEntry;
        var deletedInstanceList = new List<InstanceMetadata>();
        for (int i = 0; i < numberOfResults; i++)
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            deletedInstanceList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid, i, partitionEntry), instanceProperties));
        }

        return deletedInstanceList;
    }
}
