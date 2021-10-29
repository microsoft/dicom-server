// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store
{
    public class StoreOrchestratorTests
    {
        private const string DefaultStudyInstanceUid = "1";
        private const string DefaultSeriesInstanceUid = "2";
        private const string DefaultSopInstanceUid = "3";
        private const long DefaultVersion = 1;
        private static readonly VersionedInstanceIdentifier DefaultVersionedInstanceIdentifier = new VersionedInstanceIdentifier(
            DefaultStudyInstanceUid,
            DefaultSeriesInstanceUid,
            DefaultSopInstanceUid,
            DefaultVersion);

        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly IFileStore _fileStore = Substitute.For<IFileStore>();
        private readonly IMetadataStore _metadataStore = Substitute.For<IMetadataStore>();
        private readonly IIndexDataStore _indexDataStore = Substitute.For<IIndexDataStore>();
        private readonly IDeleteService _deleteService = Substitute.For<IDeleteService>();
        private readonly IQueryTagService _queryTagService = Substitute.For<IQueryTagService>();
        private readonly IDicomRequestContextAccessor _contextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        private readonly StoreOrchestrator _storeOrchestrator;

        private readonly DicomDataset _dicomDataset;
        private readonly Stream _stream = new MemoryStream();
        private readonly IDicomInstanceEntry _dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();
        private readonly List<QueryTagsExpiredEventArgs> _eventInvocations = new List<QueryTagsExpiredEventArgs>();
        private readonly List<QueryTag> _queryTags = new List<QueryTag>
        {
            new QueryTag(new ExtendedQueryTagStoreEntry(1, "00101010", "AS", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0))
        };

        public StoreOrchestratorTests()
        {
            _dicomDataset = new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, DefaultStudyInstanceUid },
                { DicomTag.SeriesInstanceUID, DefaultSeriesInstanceUid },
                { DicomTag.SOPInstanceUID, DefaultSopInstanceUid },
            };

            _dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset);
            _dicomInstanceEntry.GetStreamAsync(DefaultCancellationToken).Returns(_stream);

            _indexDataStore
                .BeginCreateInstanceIndexAsync(Arg.Any<int>(), _dicomDataset, Arg.Any<IEnumerable<QueryTag>>(), DefaultCancellationToken)
                .Returns(DefaultVersion);

            _queryTagService
                .GetQueryTagsAsync(Arg.Any<CancellationToken>())
                .Returns(_queryTags);

            _contextAccessor.RequestContext.DataPartitionEntry = new PartitionEntry(1, "Microsoft.Default");

            _storeOrchestrator = new StoreOrchestrator(
                _contextAccessor,
                _fileStore,
                _metadataStore,
                _indexDataStore,
                _deleteService,
                _queryTagService);
        }

        [Fact]
        public async Task GivenFilesAreSuccessfullyStored_WhenStoringFile_ThenStatusShouldBeUpdatedToCreated()
        {
            await _storeOrchestrator.StoreDicomInstanceEntryAsync(_dicomInstanceEntry, DefaultCancellationToken);

            await ValidateStatusUpdateAsync();
        }

        [Fact]
        public async Task GivenFailedToStoreFile_WhenStoringFile_ThenCleanupShouldBeAttempted()
        {
            _fileStore.StoreFileAsync(
                Arg.Is<VersionedInstanceIdentifier>(identifier => DefaultVersionedInstanceIdentifier.Equals(identifier)),
                _stream,
                cancellationToken: DefaultCancellationToken)
                .Throws(new Exception());

            _indexDataStore.ClearReceivedCalls();

            await Assert.ThrowsAsync<Exception>(() => _storeOrchestrator.StoreDicomInstanceEntryAsync(_dicomInstanceEntry, DefaultCancellationToken));

            await ValidateCleanupAsync();

            await _indexDataStore.DidNotReceiveWithAnyArgs().EndCreateInstanceIndexAsync(default, default, default, default, default);
        }

        [Fact]
        public async Task GivenFailedToStoreMetadataFile_WhenStoringMetadata_ThenCleanupShouldBeAttempted()
        {
            _metadataStore.StoreInstanceMetadataAsync(
                _dicomDataset,
                DefaultVersion,
                DefaultCancellationToken)
                .Throws(new Exception());

            _indexDataStore.ClearReceivedCalls();

            await Assert.ThrowsAsync<Exception>(() => _storeOrchestrator.StoreDicomInstanceEntryAsync(_dicomInstanceEntry, DefaultCancellationToken));

            await ValidateCleanupAsync();

            await _indexDataStore.DidNotReceiveWithAnyArgs().EndCreateInstanceIndexAsync(default, default, default, default, default);
        }

        [Fact]
        public async Task GivenExceptionDuringCleanup_WhenStoreDicomInstanceEntryIsCalled_ThenItShouldNotInterfere()
        {
            _metadataStore.StoreInstanceMetadataAsync(
                _dicomDataset,
                DefaultVersion,
                DefaultCancellationToken)
                .Throws(new ArgumentException());

            _indexDataStore.DeleteInstanceIndexAsync(default, default, default, default, default).ThrowsForAnyArgs(new InvalidOperationException());

            await Assert.ThrowsAsync<ArgumentException>(() => _storeOrchestrator.StoreDicomInstanceEntryAsync(_dicomInstanceEntry, DefaultCancellationToken));
        }

        private Task ValidateStatusUpdateAsync()
            => ValidateStatusUpdateAsync(_queryTags);

        private Task ValidateStatusUpdateAsync(IEnumerable<QueryTag> expectedTags)
            => _indexDataStore
                .Received(1)
                .EndCreateInstanceIndexAsync(
                    1,
                    _dicomDataset,
                    DefaultVersionedInstanceIdentifier.Version,
                    expectedTags,
                    false,
                    DefaultCancellationToken);

        private Task ValidateCleanupAsync()
            => _deleteService
                .Received(1)
                .DeleteInstanceNowAsync(
                    DefaultStudyInstanceUid,
                    DefaultSeriesInstanceUid,
                    DefaultSopInstanceUid,
                    CancellationToken.None);
    }
}
