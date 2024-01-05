// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
#if NET8_0_OR_GREATER
using Microsoft.Extensions.Time.Testing;
#endif
using Microsoft.Health.Abstractions.Features.Transactions;
#if !NET8_0_OR_GREATER
using Microsoft.Health.Core.Internal;
#endif
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models.Delete;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Delete;

public class DeleteServiceTests
{
    private readonly DeleteService _deleteService;
    private readonly DeleteService _deleteServiceWithExternalStore;
    private readonly IIndexDataStore _indexDataStore;
    private readonly IFileStore _fileDataStore;
    private readonly ITransactionScope _transactionScope;
    private readonly DeletedInstanceCleanupConfiguration _deleteConfiguration;
    private readonly IMetadataStore _metadataStore;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly TelemetryClient _telemetryClient;
    private readonly FileProperties _defaultFileProperties = new FileProperties
    {
        Path = "partitionA/123.dcm",
        ETag = "e45678",
        ContentLength = 123
    };

#if NET8_0_OR_GREATER
    private readonly FakeTimeProvider _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
#endif

    public DeleteServiceTests()
    {
        _telemetryClient = new TelemetryClient(new TelemetryConfiguration());
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
        _dicomRequestContextAccessor.RequestContext.DataPartition = Partition.Default;

        IOptions<FeatureConfiguration> _options = Substitute.For<IOptions<FeatureConfiguration>>();
        _options.Value.Returns(new FeatureConfiguration { EnableExternalStore = false });
        _deleteService = new DeleteService(
            _indexDataStore,
            _metadataStore,
            _fileDataStore,
            deletedInstanceCleanupConfigurationOptions,
            transactionHandler,
            NullLogger<DeleteService>.Instance,
            _dicomRequestContextAccessor,
            _options,
#if NET8_0_OR_GREATER
            _telemetryClient,
            _timeProvider);
#else
            _telemetryClient);
#endif

        IOptions<FeatureConfiguration> _optionsExternalStoreEnabled = Substitute.For<IOptions<FeatureConfiguration>>();
        _optionsExternalStoreEnabled.Value.Returns(new FeatureConfiguration { EnableExternalStore = true, });
        _deleteServiceWithExternalStore = new DeleteService(
            _indexDataStore,
            _metadataStore,
            _fileDataStore,
            deletedInstanceCleanupConfigurationOptions,
            transactionHandler,
            NullLogger<DeleteService>.Instance,
            _dicomRequestContextAccessor,
            _optionsExternalStoreEnabled,
#if NET8_0_OR_GREATER
            _telemetryClient,
            _timeProvider);
#else
            _telemetryClient);
#endif
    }

    [Fact]
    public async Task GivenADeleteStudyRequest_WhenDataStoreIsCalled_ThenCorrectDeleteDelayIsUsed()
    {
        string studyInstanceUid = TestUidGenerator.Generate();

        DateTimeOffset now = DateTimeOffset.UtcNow;
#if NET8_0_OR_GREATER
        _timeProvider.SetUtcNow(now);
#else
        IDisposable replacement = Mock.Property(() => ClockResolver.UtcNowFunc, () => now);
#endif
        await _deleteService.DeleteStudyAsync(studyInstanceUid, CancellationToken.None);
        await _indexDataStore
            .Received(1)
            .DeleteStudyIndexAsync(Partition.Default, studyInstanceUid, now + _deleteConfiguration.DeleteDelay);
    }

    [Fact]
    public async Task GivenADeleteStudyRequest_WhenDataStoreWithExternalStoreIsCalled_ThenNoDelayIsUsed()
    {
        string studyInstanceUid = TestUidGenerator.Generate();

        DateTimeOffset now = DateTimeOffset.UtcNow;
#if NET8_0_OR_GREATER
        _timeProvider.SetUtcNow(now);
#else
        IDisposable replacement = Mock.Property(() => ClockResolver.UtcNowFunc, () => now);
#endif
        await _deleteServiceWithExternalStore.DeleteStudyAsync(studyInstanceUid, CancellationToken.None);
        await _indexDataStore
            .Received(1)
            .DeleteStudyIndexAsync(Partition.Default, studyInstanceUid, now);
    }

    [Fact]
    public async Task GivenADeleteSeriesRequest_WhenDataStoreIsCalled_ThenCorrectDeleteDelayIsUsed()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();

        DateTimeOffset now = DateTimeOffset.UtcNow;
#if NET8_0_OR_GREATER
        _timeProvider.SetUtcNow(now);
#else
        IDisposable replacement = Mock.Property(() => ClockResolver.UtcNowFunc, () => now);
#endif
        await _deleteService.DeleteSeriesAsync(studyInstanceUid, seriesInstanceUid, CancellationToken.None);
        await _indexDataStore
            .Received(1)
            .DeleteSeriesIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, now + _deleteConfiguration.DeleteDelay);
    }

    [Fact]
    public async Task GivenADeleteSeriesRequest_WhenDataStoreWithExternalStoreIsCalled_ThenNoDeleteDelayIsUsed()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();

        DateTimeOffset now = DateTimeOffset.UtcNow;
#if NET8_0_OR_GREATER
        _timeProvider.SetUtcNow(now);
#else
        IDisposable replacement = Mock.Property(() => ClockResolver.UtcNowFunc, () => now);
#endif
        await _deleteServiceWithExternalStore.DeleteSeriesAsync(studyInstanceUid, seriesInstanceUid, CancellationToken.None);
        await _indexDataStore
            .Received(1)
            .DeleteSeriesIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, now);
    }

    [Fact]
    public async Task GivenADeleteInstanceRequest_WhenDataStoreIsCalled_ThenCorrectDeleteDelayIsUsed()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        DateTimeOffset now = DateTimeOffset.UtcNow;
#if NET8_0_OR_GREATER
        _timeProvider.SetUtcNow(now);
#else
        IDisposable replacement = Mock.Property(() => ClockResolver.UtcNowFunc, () => now);
#endif
        await _deleteService.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, CancellationToken.None);
        await _indexDataStore
            .Received(1)
            .DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid, now + _deleteConfiguration.DeleteDelay);
    }

    [Fact]
    public async Task GivenADeleteInstanceRequest_WhenNoInstancesFoundToDelete_ExpectNoExceptionsThrown()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        _indexDataStore
            .DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VersionedInstanceIdentifier>());

        await _deleteService.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid,
            CancellationToken.None);
    }

    [Fact]
    public async Task GivenADeleteInstanceRequest_WhenDataStoreWithExternalStoreIsCalled_ThenCorrectDelayIsUsed()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        DateTimeOffset now = DateTimeOffset.UtcNow;
#if NET8_0_OR_GREATER
        _timeProvider.SetUtcNow(now);
#else
        IDisposable replacement = Mock.Property(() => ClockResolver.UtcNowFunc, () => now);
#endif
        await _deleteServiceWithExternalStore.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, CancellationToken.None);
        await _indexDataStore
            .Received(1)
            .DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid, now);
    }

    [Fact]
    public async Task GivenADeleteInstanceRequestWithNonDefaultPartition_WhenDataStoreIsCalled_ThenNonDefaultPartitionIsUsed()
    {
        List<InstanceMetadata> responseList = GeneratedDeletedInstanceList(1, partition: new Partition(123, "ANonDefaultName"));

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
            .DeleteFileIfExistsAsync(version: default, Partition.Default, _defaultFileProperties, CancellationToken.None);

        await _metadataStore
            .DidNotReceiveWithAnyArgs()
            .DeleteInstanceMetadataIfExistsAsync(version: default, CancellationToken.None);

        _transactionScope.Received(1).Complete();
    }

    [Fact]
    public async Task GivenADeletedInstance_WhenFileStoreThrows_ThenIncrementRetryIsCalled()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
#if NET8_0_OR_GREATER
        _timeProvider.SetUtcNow(now);
#else
        IDisposable replacement = Mock.Property(() => ClockResolver.UtcNowFunc, () => now);
#endif
        List<InstanceMetadata> responseList = GeneratedDeletedInstanceList(1);

        _indexDataStore
            .RetrieveDeletedInstancesWithPropertiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(responseList);

        _fileDataStore
            .DeleteFileIfExistsAsync(Arg.Any<long>(), Partition.Default, _defaultFileProperties, Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(new Exception("Generic exception"));

        (bool success, int retrievedInstanceCount) = await _deleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

        Assert.True(success);
        Assert.Equal(1, retrievedInstanceCount);

        await _indexDataStore
            .Received(1)
            .IncrementDeletedInstanceRetryAsync(responseList[0].VersionedInstanceIdentifier, now + _deleteConfiguration.RetryBackOff, CancellationToken.None);
    }

    [Fact]
    public async Task GivenADeletedInstanceWithExternalStore_WhenFileStoreThrows_ThenIncrementRetryIsCalled()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
#if NET8_0_OR_GREATER
        _timeProvider.SetUtcNow(now);
#else
        IDisposable replacement = Mock.Property(() => ClockResolver.UtcNowFunc, () => now);
#endif

        List<InstanceMetadata> responseList = GeneratedDeletedInstanceList(1);

        _indexDataStore
            .RetrieveDeletedInstancesWithPropertiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(responseList);

        _fileDataStore
            .DeleteFileIfExistsAsync(Arg.Any<long>(), Partition.Default, _defaultFileProperties, Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(new Exception("Generic exception"));

        (bool success, int retrievedInstanceCount) = await _deleteServiceWithExternalStore.CleanupDeletedInstancesAsync(CancellationToken.None);

        Assert.True(success);
        Assert.Equal(1, retrievedInstanceCount);

        await _indexDataStore
            .Received(1)
            .IncrementDeletedInstanceRetryAsync(responseList[0].VersionedInstanceIdentifier, now + _deleteConfiguration.RetryBackOff, CancellationToken.None);
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
            .DeleteFileIfExistsAsync(Arg.Any<long>(), Partition.Default, _defaultFileProperties, Arg.Any<CancellationToken>())
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
            .DeleteFileIfExistsAsync(version: default, Partition.Default, _defaultFileProperties, CancellationToken.None);
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

    [Fact]
    public async Task GivenMultipleDeletedInstancePreviouslyUpdatedAndNowWithOriginalWatermark_WhenCleanupCalledWithoutExternalStore_ThenMethodsAreCalledWhileUsingOriginalWatermark()
    {
        List<InstanceMetadata> responseList =
            GeneratedDeletedInstanceList(
                2,
                new InstanceProperties { OriginalVersion = 1, NewVersion = null },
                generateUniqueFileProperties: false).Take(1).ToList();

        // ensure no instances contain file properties
        Assert.DoesNotContain(responseList, i => i.InstanceProperties.FileProperties != null);

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

            // delete both original and new version's metadata
            await _metadataStore
                .Received(1)
                .DeleteInstanceMetadataIfExistsAsync(deletedVersion.Version, CancellationToken.None);
            await _metadataStore
                .Received(1)
                .DeleteInstanceMetadataIfExistsAsync(instance.InstanceProperties.OriginalVersion.Value, CancellationToken.None);

            Assert.Null(instance.InstanceProperties.FileProperties);

            // delete both original and new version's blobs and fileProperties are null and not used
            await _fileDataStore
                .Received(1)
                .DeleteFileIfExistsAsync(deletedVersion.Version, deletedVersion.Partition, fileProperties: null, cancellationToken: CancellationToken.None);

            await _fileDataStore
               .Received(1)
               .DeleteFileIfExistsAsync(instance.InstanceProperties.OriginalVersion.Value, deletedVersion.Partition, fileProperties: null, CancellationToken.None);
        }

        await _indexDataStore
            .DidNotReceiveWithAnyArgs()
            .IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier: default, cleanupAfter: default, CancellationToken.None);

        _transactionScope.Received(1).Complete();
    }

    [Fact]
    public async Task GivenMultipleDeletedInstanceWithExternalStore_WhenCleanupCalled_ThenMethodsAreCalledWithNonNullFileProperties()
    {
        List<InstanceMetadata> responseList =
            GeneratedDeletedInstanceList(2, generateUniqueFileProperties: true);

        // ensure instances contain file properties that are not null
        Assert.DoesNotContain(responseList, i => i.InstanceProperties.FileProperties == null);

        _indexDataStore
            .RetrieveDeletedInstancesWithPropertiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(responseList);

        (bool success, int retrievedInstanceCount) = await _deleteServiceWithExternalStore.CleanupDeletedInstancesAsync(CancellationToken.None);

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

            Assert.NotNull(instance.InstanceProperties.FileProperties);

            await _fileDataStore
               .Received(1)
               .DeleteFileIfExistsAsync(deletedVersion.Version, deletedVersion.Partition, instance.InstanceProperties.FileProperties, CancellationToken.None);
        }

        await _indexDataStore
            .DidNotReceiveWithAnyArgs()
            .IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier: default, cleanupAfter: default, CancellationToken.None);

        _transactionScope.Received(1).Complete();
    }

    [Fact]
    public async Task GivenAvailableDatabase_WhenFetchingMetrics_ThenSuccessfullyFetch()
    {
        const int ExhaustedRetries = 42;
        DateTimeOffset oldestTimestamp = DateTimeOffset.UtcNow.AddMonths(-1);

        using CancellationTokenSource source = new();

        _indexDataStore.GetOldestDeletedAsync(source.Token).Returns(oldestTimestamp);
        _indexDataStore
            .RetrieveNumExhaustedDeletedInstanceAttemptsAsync(_deleteConfiguration.MaxRetries, source.Token)
            .Returns(ExhaustedRetries);

        DeleteMetrics actual = await _deleteService.GetMetricsAsync(source.Token);

        Assert.Equal(oldestTimestamp, actual.OldestDeletion);
        Assert.Equal(ExhaustedRetries, actual.TotalExhaustedRetries);
    }

    private async Task ValidateSuccessfulCleanupDeletedInstanceCall(bool success, IReadOnlyCollection<VersionedInstanceIdentifier> responseList, int retrievedInstanceCount, FileProperties expectedFileProperties = null)
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
                .DeleteFileIfExistsAsync(deletedVersion.Version, deletedVersion.Partition, expectedFileProperties, CancellationToken.None);
        }

        await _indexDataStore
            .DidNotReceiveWithAnyArgs()
            .IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier: default, cleanupAfter: default, CancellationToken.None);

        _transactionScope.Received(1).Complete();
    }

    private static List<InstanceMetadata> GeneratedDeletedInstanceList(int numberOfResults, InstanceProperties instanceProperties = null, Partition partition = null, FileProperties fileProperties = null, bool generateUniqueFileProperties = false)
    {
        if (generateUniqueFileProperties)
        {
            fileProperties = new FileProperties
            {
                Path = Guid.NewGuid() + ".dcm",
                ETag = "e" + Guid.NewGuid(),
                ContentLength = 123
            };
        }
        instanceProperties ??= new InstanceProperties() { FileProperties = fileProperties };
        partition ??= Partition.Default;
        var deletedInstanceList = new List<InstanceMetadata>();
        for (int i = 0; i < numberOfResults; i++)
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            deletedInstanceList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid, i, partition), instanceProperties));
        }

        return deletedInstanceList;
    }
}
