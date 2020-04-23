// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Features.Transactions;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Delete
{
    public class DicomDeleteServiceTests
    {
        private readonly DicomDeleteService _dicomDeleteService;
        private readonly IDicomIndexDataStore _dicomIndexDataStore;
        private readonly IDicomFileStore _dicomFileDataStore;
        private readonly ITransactionScope _transactionScope;
        private readonly DeletedInstanceCleanupConfiguration _dicomDeleteConfiguration;
        private readonly IDicomMetadataStore _dicomMetadataStore;

        public DicomDeleteServiceTests()
        {
            _dicomIndexDataStore = Substitute.For<IDicomIndexDataStore>();
            _dicomMetadataStore = Substitute.For<IDicomMetadataStore>();
            _dicomFileDataStore = Substitute.For<IDicomFileStore>();
            _dicomDeleteConfiguration = new DeletedInstanceCleanupConfiguration
            {
                DeleteDelay = TimeSpan.FromSeconds(1),
                BatchSize = 10,
                MaxRetries = 5,
                PollingInterval = TimeSpan.FromSeconds(1),
                RetryBackOff = TimeSpan.FromSeconds(60),
            };

            IOptions<DeletedInstanceCleanupConfiguration> deletedInstanceCleanupConfigurationOptions = Substitute.For<IOptions<DeletedInstanceCleanupConfiguration>>();
            deletedInstanceCleanupConfigurationOptions.Value.Returns(_dicomDeleteConfiguration);
            ITransactionHandler transactionHandler = Substitute.For<ITransactionHandler>();
            _transactionScope = Substitute.For<ITransactionScope>();
            transactionHandler.BeginTransaction().Returns(_transactionScope);

            _dicomDeleteService = new DicomDeleteService(_dicomIndexDataStore, _dicomMetadataStore, _dicomFileDataStore, deletedInstanceCleanupConfigurationOptions, transactionHandler, NullLogger<DicomDeleteService>.Instance);
        }

        [Fact]
        public async Task GivenNoDeletedInstances_WhenCleanupCalled_ThenNotCallStoresAndReturnsCorrectTuple()
        {
            _dicomIndexDataStore
                .RetrieveDeletedInstancesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(Enumerable.Empty<VersionedDicomInstanceIdentifier>());

            (bool success, int retrievedInstanceCount) = await _dicomDeleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

            Assert.True(success);
            Assert.Equal(0, retrievedInstanceCount);

            await _dicomIndexDataStore
                .ReceivedWithAnyArgs(1)
                .RetrieveDeletedInstancesAsync(cleanupAfter: default, batchSize: default, maxRetries: default, CancellationToken.None);

            await _dicomIndexDataStore
                .DidNotReceiveWithAnyArgs()
                .DeleteDeletedInstanceAsync(versionedInstanceIdentifier: default, CancellationToken.None);

            await _dicomIndexDataStore
                .DidNotReceiveWithAnyArgs()
                .IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier: default, cleanupAfter: default, CancellationToken.None);

            await _dicomFileDataStore
                .DidNotReceiveWithAnyArgs()
                .DeleteFileIfExistsAsync(dicomInstanceIdentifier: default, CancellationToken.None);

            await _dicomMetadataStore
                .DidNotReceiveWithAnyArgs()
                .DeleteInstanceMetadataIfExistsAsync(dicomInstanceIdentifier: default, CancellationToken.None);

            _transactionScope.Received(1).Complete();
        }

        [Fact]
        public async Task GivenADeletedInstance_WhenFileStoreThrows_ThenIncrementRetryIsCalled()
        {
            List<VersionedDicomInstanceIdentifier> responseList = GeneratedDeletedInstanceList(1);

            _dicomIndexDataStore
                .RetrieveDeletedInstancesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(responseList);

            _dicomFileDataStore
                .DeleteFileIfExistsAsync(Arg.Any<VersionedDicomInstanceIdentifier>(), Arg.Any<CancellationToken>())
                .ThrowsForAnyArgs(new Exception("Generic exception"));

            (bool success, int retrievedInstanceCount) = await _dicomDeleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

            Assert.True(success);
            Assert.Equal(1, retrievedInstanceCount);

            await _dicomIndexDataStore
                .Received(1)
                .IncrementDeletedInstanceRetryAsync(responseList[0], cleanupAfter: Arg.Any<DateTimeOffset>(), CancellationToken.None);
        }

        [Fact]
        public async Task GivenADeletedInstance_WhenMetadataStoreThrowsUnhandled_ThenIncrementRetryIsCalled()
        {
            List<VersionedDicomInstanceIdentifier> responseList = GeneratedDeletedInstanceList(1);

            _dicomIndexDataStore
                .RetrieveDeletedInstancesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(responseList);

            _dicomMetadataStore
                .DeleteInstanceMetadataIfExistsAsync(Arg.Any<VersionedDicomInstanceIdentifier>(), Arg.Any<CancellationToken>())
                .ThrowsForAnyArgs(new Exception("Generic exception"));

            (bool success, int retrievedInstanceCount) = await _dicomDeleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

            Assert.True(success);
            Assert.Equal(1, retrievedInstanceCount);

            await _dicomIndexDataStore
                .Received(1)
                .IncrementDeletedInstanceRetryAsync(responseList[0], cleanupAfter: Arg.Any<DateTimeOffset>(), CancellationToken.None);
        }

        [Fact]
        public async Task GivenADeletedInstance_WhenIncrementThrows_ThenSuccessIsReturnedFalse()
        {
            List<VersionedDicomInstanceIdentifier> responseList = GeneratedDeletedInstanceList(1);

            _dicomIndexDataStore
                .RetrieveDeletedInstancesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(responseList);

            _dicomFileDataStore
                .DeleteFileIfExistsAsync(Arg.Any<VersionedDicomInstanceIdentifier>(), Arg.Any<CancellationToken>())
                .ThrowsForAnyArgs(new Exception("Generic exception"));

            _dicomIndexDataStore
                .IncrementDeletedInstanceRetryAsync(Arg.Any<VersionedDicomInstanceIdentifier>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
                .ThrowsForAnyArgs(new Exception("Generic exception"));

            (bool success, int retrievedInstanceCount) = await _dicomDeleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

            Assert.False(success);
            Assert.Equal(1, retrievedInstanceCount);

            await _dicomIndexDataStore
                .Received(1)
                .IncrementDeletedInstanceRetryAsync(responseList[0], cleanupAfter: Arg.Any<DateTimeOffset>(), CancellationToken.None);
        }

        [Fact]
        public async Task GivenADeletedInstance_WhenRetrieveThrows_ThenSuccessIsReturnedFalse()
        {
            List<VersionedDicomInstanceIdentifier> responseList = GeneratedDeletedInstanceList(1);

            _dicomIndexDataStore
                .RetrieveDeletedInstancesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ThrowsForAnyArgs(new Exception("Generic exception"));

            (bool success, int retrievedInstanceCount) = await _dicomDeleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

            Assert.False(success);
            Assert.Equal(0, retrievedInstanceCount);

            await _dicomIndexDataStore
                .DidNotReceiveWithAnyArgs()
                .DeleteDeletedInstanceAsync(versionedInstanceIdentifier: default, CancellationToken.None);

            await _dicomIndexDataStore
                .DidNotReceiveWithAnyArgs()
                .IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier: default, cleanupAfter: default, CancellationToken.None);

            await _dicomFileDataStore
                .DidNotReceiveWithAnyArgs()
                .DeleteFileIfExistsAsync(dicomInstanceIdentifier: default, CancellationToken.None);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public async Task GivenMultipleDeletedInstance_WhenCleanupCalled_ThenCorrectMethodsAreCalledAndReturnsCorrectTuple(int numberOfDeletedInstances)
        {
            List<VersionedDicomInstanceIdentifier> responseList = GeneratedDeletedInstanceList(numberOfDeletedInstances);

            _dicomIndexDataStore
                .RetrieveDeletedInstancesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(responseList);

            (bool success, int retrievedInstanceCount) = await _dicomDeleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

            await ValidateSuccessfulCleanupDeletedInstanceCall(success, responseList, retrievedInstanceCount);
        }

        private async Task ValidateSuccessfulCleanupDeletedInstanceCall(bool success, IReadOnlyCollection<VersionedDicomInstanceIdentifier> responseList, int retrievedInstanceCount)
        {
            Assert.True(success);
            Assert.Equal(responseList.Count, retrievedInstanceCount);

            await _dicomIndexDataStore
                .ReceivedWithAnyArgs(1)
                .RetrieveDeletedInstancesAsync(default, default, default, CancellationToken.None);

            foreach (VersionedDicomInstanceIdentifier deletedVersion in responseList)
            {
                await _dicomIndexDataStore
                    .Received(1)
                    .DeleteDeletedInstanceAsync(deletedVersion, CancellationToken.None);

                await _dicomMetadataStore
                    .Received(1)
                    .DeleteInstanceMetadataIfExistsAsync(deletedVersion, CancellationToken.None);

                await _dicomFileDataStore
                    .Received(1)
                    .DeleteFileIfExistsAsync(deletedVersion, CancellationToken.None);
            }

            await _dicomIndexDataStore
                .DidNotReceiveWithAnyArgs()
                .IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier: default, cleanupAfter: default, CancellationToken.None);

            _transactionScope.Received(1).Complete();
        }

        private static List<VersionedDicomInstanceIdentifier> GeneratedDeletedInstanceList(int numberOfResults)
        {
            var deletedInstanceList = new List<VersionedDicomInstanceIdentifier>();
            for (int i = 0; i < numberOfResults; i++)
            {
                string studyInstanceUid = TestUidGenerator.Generate();
                string seriesInstanceUid = TestUidGenerator.Generate();
                string sopInstanceUid = TestUidGenerator.Generate();
                deletedInstanceList.Add(new VersionedDicomInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid, i));
            }

            return deletedInstanceList;
        }
    }
}
