// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store
{
    public class DicomStoreOrchestratorTests
    {
        private const string DefaultStudyInstanceUid = "1";
        private const string DefaultSeriesInstanceUid = "2";
        private const string DefaultSopInstanceUid = "3";
        private const long DefaultVersion = 1;
        private static readonly VersionedDicomInstanceIdentifier DefaultVersionedDicomInstanceIdentifier = new VersionedDicomInstanceIdentifier(
            DefaultStudyInstanceUid,
            DefaultSeriesInstanceUid,
            DefaultSopInstanceUid,
            DefaultVersion);

        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly IDicomFileStore _dicomFileStore = Substitute.For<IDicomFileStore>();
        private readonly IDicomMetadataStore _dicomMetadataStore = Substitute.For<IDicomMetadataStore>();
        private readonly IDicomIndexDataStore _dicomIndexDataStore = Substitute.For<IDicomIndexDataStore>();
        private readonly DicomStoreOrchestrator _dicomStoreOrchestrator;

        private readonly DicomDataset _dicomDataset;
        private readonly Stream _stream = new MemoryStream();
        private readonly IDicomInstanceEntry _dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        public DicomStoreOrchestratorTests()
        {
            _dicomDataset = new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, DefaultStudyInstanceUid },
                { DicomTag.SeriesInstanceUID, DefaultSeriesInstanceUid },
                { DicomTag.SOPInstanceUID, DefaultSopInstanceUid },
            };

            _dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset);
            _dicomInstanceEntry.GetStreamAsync(DefaultCancellationToken).Returns(_stream);

            _dicomIndexDataStore.CreateInstanceIndexAsync(_dicomDataset, DefaultCancellationToken).Returns(DefaultVersion);

            _dicomStoreOrchestrator = new DicomStoreOrchestrator(_dicomFileStore, _dicomMetadataStore, _dicomIndexDataStore);
        }

        [Fact]
        public async Task GivenFilesAreSuccessfullyStored_WhenStoreDicomInstanceEntryIsCalled_ThenStatusShouldBeUpdatedToCreated()
        {
            await _dicomStoreOrchestrator.StoreDicomInstanceEntryAsync(_dicomInstanceEntry, DefaultCancellationToken);

            await _dicomIndexDataStore.Received(1).UpdateInstanceIndexStatusAsync(
                Arg.Is<VersionedDicomInstanceIdentifier>(identifier => DefaultVersionedDicomInstanceIdentifier.Equals(identifier)),
                DicomIndexStatus.Created,
                DefaultCancellationToken);
        }

        [Fact]
        public async Task GivenFailedToStoreFile_WhenStoreDicomInstanceEntryIsCalled_ThenCleanupShouldBeAttempted()
        {
            _dicomFileStore.AddFileAsync(
                Arg.Is<VersionedDicomInstanceIdentifier>(identifier => DefaultVersionedDicomInstanceIdentifier.Equals(identifier)),
                _stream,
                overwriteIfExists: false,
                cancellationToken: DefaultCancellationToken)
                .Throws(new Exception());

            _dicomIndexDataStore.ClearReceivedCalls();

            await Assert.ThrowsAsync<Exception>(() => _dicomStoreOrchestrator.StoreDicomInstanceEntryAsync(_dicomInstanceEntry, DefaultCancellationToken));

            await ValidateCleanupAsync();

            await _dicomIndexDataStore.DidNotReceiveWithAnyArgs().UpdateInstanceIndexStatusAsync(default, default, default);
        }

        [Fact]
        public async Task GivenFailedToStoreMetadataFile_WhenStoreDicomInstanceEntryIsCalled_ThenCleanupShouldBeAttempted()
        {
            _dicomMetadataStore.AddInstanceMetadataAsync(
                _dicomDataset,
                DefaultVersion,
                DefaultCancellationToken)
                .Throws(new Exception());

            _dicomIndexDataStore.ClearReceivedCalls();

            await Assert.ThrowsAsync<Exception>(() => _dicomStoreOrchestrator.StoreDicomInstanceEntryAsync(_dicomInstanceEntry, DefaultCancellationToken));

            await ValidateCleanupAsync();

            await _dicomIndexDataStore.DidNotReceiveWithAnyArgs().UpdateInstanceIndexStatusAsync(default, default, default);
        }

        [Fact]
        public async Task GivenExceptionDuringCleanup_WhenStoreDicomInstanceEntryIsCalled_ThenItShouldNotInterfere()
        {
            _dicomMetadataStore.AddInstanceMetadataAsync(
                _dicomDataset,
                DefaultVersion,
                DefaultCancellationToken)
                .Throws(new ArgumentException());

            _dicomIndexDataStore.DeleteInstanceIndexAsync(default, default, default, default, default, default).ThrowsForAnyArgs(new InvalidOperationException());

            await Assert.ThrowsAsync<ArgumentException>(() => _dicomStoreOrchestrator.StoreDicomInstanceEntryAsync(_dicomInstanceEntry, DefaultCancellationToken));
        }

        private async Task ValidateCleanupAsync()
        {
            var timeout = DateTime.Now.AddSeconds(5);

            while (timeout < DateTime.Now)
            {
                if (_dicomIndexDataStore.ReceivedCalls().Any())
                {
                    await _dicomIndexDataStore.Received(1).DeleteInstanceIndexAsync(
                        DefaultStudyInstanceUid,
                        DefaultSeriesInstanceUid,
                        DefaultSopInstanceUid,
                        Arg.Any<DateTimeOffset>(),
                        Arg.Any<DateTimeOffset>(),
                        CancellationToken.None);

                    break;
                }

                await Task.Delay(100);
            }
        }
    }
}
