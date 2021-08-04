// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class ExtendedQueryTagErrorStoreTests : IClassFixture<SqlDataStoreTestsFixture>, IAsyncLifetime
    {
        private readonly IExtendedQueryTagErrorStore _extendedQueryTagErrorStore;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IIndexDataStore _indexDataStore;
        private readonly DateTime _definedNow;
        private long _watermark;

        public ExtendedQueryTagErrorStoreTests(SqlDataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _extendedQueryTagStore = EnsureArg.IsNotNull(fixture.ExtendedQueryTagStore, nameof(fixture.ExtendedQueryTagStore));
            _extendedQueryTagErrorStore = EnsureArg.IsNotNull(fixture.ExtendedQueryTagErrorStore, nameof(fixture.ExtendedQueryTagErrorStore));
            _indexDataStore = EnsureArg.IsNotNull(fixture.IndexDataStore, nameof(fixture.IndexDataStore));
            _definedNow = DateTime.UtcNow;
        }
        public async Task DisposeAsync()
        {
            await CleanupTagErrorsAsync();
        }

        private async Task CleanupTagErrorsAsync()
        {
            var tags = await _extendedQueryTagStore.GetExtendedQueryTagsAsync();
            foreach (var tag in tags)
            {
                await _extendedQueryTagErrorStore.DeleteExtendedQueryTagErrorsAsync(tag.Path);
                await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(tag.Path, tag.VR);
            }
        }

        private async Task<ExtendedQueryTagStoreEntry> CreateTagInStoreAsync(string studyInstanceUid = null, string seriesInstanceUid = null, string sopInstanceUid = null, CancellationToken cancellationToken = default)
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();

            DicomDataset dataset = CreateTestDicomDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            _watermark = await _indexDataStore.CreateInstanceIndexAsync(dataset);

            await _extendedQueryTagStore.AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1 }, 128, ready: true, cancellationToken: cancellationToken);

            var actualExtendedQueryTagEntries = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(extendedQueryTagEntry1.Path);

            Assert.Equal(actualExtendedQueryTagEntries[0].Path, extendedQueryTagEntry1.Path);
            return actualExtendedQueryTagEntries[0];
        }

        private static DicomDataset CreateTestDicomDataset(string studyInstanceUid = null, string seriesInstanceUid = null, string sopInstanceUid = null)
        {
            if (string.IsNullOrEmpty(studyInstanceUid))
            {
                studyInstanceUid = TestUidGenerator.Generate();
            }

            if (string.IsNullOrEmpty(seriesInstanceUid))
            {
                seriesInstanceUid = TestUidGenerator.Generate();
            }

            if (string.IsNullOrEmpty(sopInstanceUid))
            {
                sopInstanceUid = TestUidGenerator.Generate();
            }

            DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

            dataset.Remove(DicomTag.PatientID);

            dataset.Add(DicomTag.PatientID, "pid");
            dataset.Add(DicomTag.PatientName, "pname");
            dataset.Add(DicomTag.ReferringPhysicianName, "rname");
            dataset.Add(DicomTag.StudyDate, "20200301");
            dataset.Add(DicomTag.StudyDescription, "sd");
            dataset.Add(DicomTag.AccessionNumber, "an");
            dataset.Add(DicomTag.Modality, "M");
            dataset.Add(DicomTag.PerformedProcedureStepStartDate, "20200302");
            return dataset;
        }

        [Fact]
        public async Task GivenValidExtendedQueryTagError_WhenAddExtendedQueryTagError_ThenTagShouldBeAdded()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            ExtendedQueryTagStoreEntry actualTagEntry = await CreateTagInStoreAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            int actualErrorCode = 3;

            int outputTagKey = await _extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                actualTagEntry.Key,
                actualErrorCode,
                _watermark,
                _definedNow);

            Assert.Equal(outputTagKey, actualTagEntry.Key);

            var extendedQueryTagError = await _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(actualTagEntry.Path);

            Assert.Equal(extendedQueryTagError[0].CreatedTime, _definedNow);
            Assert.Equal(extendedQueryTagError[0].StudyInstanceUid, studyInstanceUid);
            Assert.Equal(extendedQueryTagError[0].SeriesInstanceUid, seriesInstanceUid);
            Assert.Equal(extendedQueryTagError[0].SopInstanceUid, sopInstanceUid);
        }

        [Fact]
        public async Task GivenNonExistingQueryTag_WhenAddExtendedQueryTagError_ThenShouldThrowException()
        {
            var extendedQueryTag = await _extendedQueryTagStore.GetExtendedQueryTagsAsync();
            Assert.Equal(0, extendedQueryTag.Count);
            await Assert.ThrowsAsync<ExtendedQueryTagNotFoundException>(() => _extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                0, 1, 0, _definedNow));
        }

        [Fact]
        public async Task GivenExistingQueryTagError_WhenDeletingExtendedQueryTagErrors_ThenShouldDeleteAllErrorsForTag()
        {
            ExtendedQueryTagStoreEntry actualTagEntry = await CreateTagInStoreAsync();

            await _extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                actualTagEntry.Key, 1, _watermark, _definedNow);

            var extendedQueryTagErrorBeforeDelete = await _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(actualTagEntry.Path);
            Assert.Equal(1, extendedQueryTagErrorBeforeDelete.Count);

            await _extendedQueryTagErrorStore.DeleteExtendedQueryTagErrorsAsync(actualTagEntry.Path);

            var extendedQueryTagErrorAfterDelete = await _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(actualTagEntry.Path);
            Assert.Equal(0, extendedQueryTagErrorAfterDelete.Count);
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
