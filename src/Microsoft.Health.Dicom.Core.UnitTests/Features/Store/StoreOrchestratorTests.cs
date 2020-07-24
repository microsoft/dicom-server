// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Models;
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
        private readonly StoreOrchestrator _storeOrchestrator;

        private readonly DicomDataset _dicomDataset;
        private readonly Stream _stream = new MemoryStream();
        private readonly IDicomInstanceEntry _dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

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

            _indexDataStore.CreateInstanceIndexAsync(_dicomDataset, DefaultCancellationToken).Returns(DefaultVersion);

            _storeOrchestrator = new StoreOrchestrator(_fileStore, _metadataStore, _indexDataStore, _deleteService);
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

            await _indexDataStore.DidNotReceiveWithAnyArgs().UpdateInstanceIndexStatusAsync(default, default, default);
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

            await _indexDataStore.DidNotReceiveWithAnyArgs().UpdateInstanceIndexStatusAsync(default, default, default);
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

        private async Task ValidateStatusUpdateAsync()
        {
            await _indexDataStore.Received(1).UpdateInstanceIndexStatusAsync(
                Arg.Is<VersionedInstanceIdentifier>(identifier => DefaultVersionedInstanceIdentifier.Equals(identifier)),
                IndexStatus.Created,
                DefaultCancellationToken);
        }

        private async Task ValidateCleanupAsync()
        {
            await _deleteService.Received(1).DeleteInstanceNowAsync(
                       DefaultStudyInstanceUid,
                       DefaultSeriesInstanceUid,
                       DefaultSopInstanceUid,
                       CancellationToken.None);
        }
    }
}
