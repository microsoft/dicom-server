// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Features.Validation;
using NSubstitute;
using NSubstitute.Core;
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
        private readonly IElementMinimumValidator _minimumValidator = Substitute.For<IElementMinimumValidator>();
        private readonly StoreOrchestrator _storeOrchestrator;

        private readonly DicomDataset _dicomDataset;
        private readonly Stream _stream = new MemoryStream();
        private readonly IDicomInstanceEntry _dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();
        private readonly List<QueryTag> _queryTags = new List<QueryTag>
        {
            new QueryTag(new ExtendedQueryTagStoreEntry(1, "00101010", "AS", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Ready, 1L))
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
                .BeginCreateInstanceIndexAsync(_dicomDataset, Arg.Any<IEnumerable<QueryTag>>(), DefaultCancellationToken)
                .Returns(DefaultVersion);

            _queryTagService
                .GetQueryTagsAsync(false, Arg.Any<CancellationToken>())
                .Returns(_queryTags);

            _storeOrchestrator = new StoreOrchestrator(
                _fileStore,
                _metadataStore,
                _indexDataStore,
                _deleteService,
                _queryTagService,
                _minimumValidator,
                Options.Create(new StoreConfiguration()));
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

            await _indexDataStore.DidNotReceiveWithAnyArgs().EndCreateInstanceIndexAsync(default, default, default, default);
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

            await _indexDataStore.DidNotReceiveWithAnyArgs().EndCreateInstanceIndexAsync(default, default, default, default);
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

        [Fact]
        public async Task GivenVersionMismatchExceptionWhenStore_WhenRetryNotExceedMax_ThenShouldSucceed()
        {
            List<QueryTag> newTags1 = _queryTags
                .Concat(new QueryTag[] { new QueryTag(new ExtendedQueryTagStoreEntry(2, "00202020", "DT", null, QueryTagLevel.Series, ExtendedQueryTagStatus.Ready, 2L)) })
                .ToList();
            List<QueryTag> newTags2 = newTags1
                .Concat(new QueryTag[] { new QueryTag(new ExtendedQueryTagStoreEntry(3, "00303030", "DA", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, 3L)) })
                .ToList();

            _queryTagService
                .GetQueryTagsAsync(true, Arg.Any<CancellationToken>())
                .Returns(newTags1, newTags2); // Return different tags
            _indexDataStore.EndCreateInstanceIndexAsync(default, default, default, default)
                .ReturnsForAnyArgs(
                    Task.FromException(new ExtendedQueryTagVersionMismatchException()),
                    Task.FromException(new ExtendedQueryTagVersionMismatchException()),
                    Task.CompletedTask);

            await _storeOrchestrator.StoreDicomInstanceEntryAsync(_dicomInstanceEntry, DefaultCancellationToken);

            await _queryTagService.Received(1).GetQueryTagsAsync(false, DefaultCancellationToken);
            await _queryTagService.Received(2).GetQueryTagsAsync(true, DefaultCancellationToken);
            await _indexDataStore.Received(1).EndCreateInstanceIndexAsync(_dicomDataset, DefaultVersion, _queryTags, DefaultCancellationToken);
            await _indexDataStore.Received(1).EndCreateInstanceIndexAsync(_dicomDataset, DefaultVersion, newTags1, DefaultCancellationToken);
            await _indexDataStore.Received(1).EndCreateInstanceIndexAsync(_dicomDataset, DefaultVersion, newTags2, DefaultCancellationToken);
        }

        [Fact]
        public async Task GivenVersionMismatchExceptionWhenStore_WhenRetryExceedMax_ThenShouldFail()
        {
            Func<CallInfo, long> exceptionFunc = new Func<CallInfo, long>(x => throw new ExtendedQueryTagVersionMismatchException());
            _indexDataStore.BeginCreateInstanceIndexAsync(default, default, default)
                .ReturnsForAnyArgs(exceptionFunc, exceptionFunc, exceptionFunc);

            await Assert.ThrowsAsync<ExtendedQueryTagVersionMismatchException>(
                () => _storeOrchestrator.StoreDicomInstanceEntryAsync(_dicomInstanceEntry, DefaultCancellationToken));
        }

        private Task ValidateStatusUpdateAsync()
            => ValidateStatusUpdateAsync(_queryTags);

        private Task ValidateStatusUpdateAsync(IEnumerable<QueryTag> expectedTags)
            => _indexDataStore
                .Received(1)
                .EndCreateInstanceIndexAsync(
                    _dicomDataset,
                    DefaultVersionedInstanceIdentifier.Version,
                    expectedTags,
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
