// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    /// Tests for ExtendedQueryTagStore
    /// </summary>
    public class ExtendedQueryTagStoreTests : IClassFixture<SqlDataStoreTestsFixture>, IAsyncLifetime
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IIndexDataStore _indexDataStore;

        private readonly IExtendedQueryTagStoreTestHelper _extendedQueryTagStoreTestHelper;

        public ExtendedQueryTagStoreTests(SqlDataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            EnsureArg.IsNotNull(fixture.ExtendedQueryTagStore, nameof(fixture.ExtendedQueryTagStore));
            EnsureArg.IsNotNull(fixture.IndexDataStore, nameof(fixture.IndexDataStore));
            EnsureArg.IsNotNull(fixture.ExtendedQueryTagStoreTestHelper, nameof(fixture.ExtendedQueryTagStoreTestHelper));
            _extendedQueryTagStore = fixture.ExtendedQueryTagStore;
            _indexDataStore = fixture.IndexDataStore;
            _extendedQueryTagStoreTestHelper = fixture.ExtendedQueryTagStoreTestHelper;
        }

        [Fact]
        public async Task GivenValidExtendedQueryTags_WhenAddExtendedQueryTag_ThenTagShouldBeAdded()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = new DicomTag(0x0405, 0x1001, "PrivateCreator1");
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry2 = tag2.BuildAddExtendedQueryTagEntry(vr: DicomVRCode.CS);
            IReadOnlyList<ExtendedQueryTagReference> added = await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 });

            await VerifyTagIsAdded(added[0].Key, extendedQueryTagEntry1);
            await VerifyTagIsAdded(added[1].Key, extendedQueryTagEntry2);
        }

        [Fact]
        public async Task GivenUnfinishedExistingExtendedQueryTag_WhenAddExtendedQueryTag_ThenTagShouldBeAdded()
        {
            DicomTag tag = DicomTag.PatientAge;
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();

            // Add and verify the tag was added
            ExtendedQueryTagReference oldTag = (await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }, ready: false)).Single();
            await VerifyTagIsAdded(oldTag.Key, extendedQueryTagEntry, ExtendedQueryTagStatus.Adding);

            // Add the tag again before it can be associated with a re-indexing operation
            ExtendedQueryTagReference newTag = (await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }, ready: false)).Single();
            await VerifyTagIsAdded(newTag.Key, extendedQueryTagEntry, ExtendedQueryTagStatus.Adding);
            Assert.NotEqual(oldTag, newTag);
        }

        [Fact]
        public async Task GivenCompletedExtendedQueryTag_WhenAddExtendedQueryTag_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();
            await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry });
            await Assert.ThrowsAsync<ExtendedQueryTagsAlreadyExistsException>(() => AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }));
        }

        [Fact]
        public async Task GivenReindexingExtendedQueryTag_WhenAddExtendedQueryTag_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();
            ExtendedQueryTagReference tagReference = (await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }, ready: false)).Single();
            Assert.NotEmpty(await _extendedQueryTagStore.AssignReindexingOperationAsync(new int[] { tagReference.Key }, Guid.NewGuid()));
            await Assert.ThrowsAsync<ExtendedQueryTagsAlreadyExistsException>(() => AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }));
        }

        [Fact]
        public async Task GivenMoreThanAllowedExtendedQueryTags_WhenAddExtendedQueryTag_ThenShouldThrowException()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1 });
            DicomTag tag2 = DicomTag.DeviceDescription;
            AddExtendedQueryTagEntry extendedQueryTagEntry2 = tag2.BuildAddExtendedQueryTagEntry();
            await Assert.ThrowsAsync<ExtendedQueryTagsExceedsMaxAllowedCountException>(() => AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry2 }, maxAllowedCount: 1));
        }

        [Fact]
        public async Task GivenExistingExtendedQueryTag_WhenDeleteExtendedQueryTag_ThenTagShouldBeRemoved()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();
            await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry });
            await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(extendedQueryTagEntry.Path, extendedQueryTagEntry.VR);
            await VerifyTagNotExist(extendedQueryTagEntry.Path);
        }

        [Fact]
        public async Task GivenNonExistingExtendedQueryTag_WhenDeleteExtendedQueryTag_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            GetExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildGetExtendedQueryTagEntry();
            await Assert.ThrowsAsync<ExtendedQueryTagNotFoundException>(() => _extendedQueryTagStore.DeleteExtendedQueryTagAsync(extendedQueryTagEntry.Path, extendedQueryTagEntry.VR));
            await VerifyTagNotExist(extendedQueryTagEntry.Path);
        }

        [Fact]
        public async Task GivenExistingExtendedQueryTagIndexData_WhenDeleteExtendedQueryTag_ThenShouldDeleteIndexData()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;

            // Prepare index data
            DicomDataset dataset = Samples.CreateRandomInstanceDataset();
            dataset.Add(tag, "123");

            await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { tag.BuildAddExtendedQueryTagEntry() });
            ExtendedQueryTagStoreEntry storeEntry = (await _extendedQueryTagStore.GetExtendedQueryTagsAsync(path: tag.GetPath()))[0];
            QueryTag queryTag = new QueryTag(storeEntry);
            await _indexDataStore.CreateInstanceIndexAsync(dataset, new QueryTag[] { queryTag });
            var extendedQueryTagIndexData = await _extendedQueryTagStoreTestHelper.GetExtendedQueryTagDataForTagKeyAsync(ExtendedQueryTagDataType.StringData, storeEntry.Key);
            Assert.NotEmpty(extendedQueryTagIndexData);

            // Delete tag
            await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(storeEntry.Path, storeEntry.VR);
            await VerifyTagNotExist(storeEntry.Path);

            // Verify index data is removed
            extendedQueryTagIndexData = await _extendedQueryTagStoreTestHelper.GetExtendedQueryTagDataForTagKeyAsync(ExtendedQueryTagDataType.StringData, storeEntry.Key);
            Assert.Empty(extendedQueryTagIndexData);
        }

        [Fact]
        public async Task GivenQueryTags_WhenGettingTagsByOperation_ThenOnlyAssignedTags()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = DicomTag.PatientAge;
            DicomTag tag3 = DicomTag.PatientMotherBirthName;

            IReadOnlyList<ExtendedQueryTagStoreEntry> actual;

            // Add the tags
            List<ExtendedQueryTagReference> expected = (await AddExtendedQueryTagsAsync(
                new AddExtendedQueryTagEntry[]
                {
                    tag1.BuildAddExtendedQueryTagEntry(),
                    tag2.BuildAddExtendedQueryTagEntry(),
                    tag3.BuildAddExtendedQueryTagEntry(),
                },
                ready: false)).Take(2).ToList();

            // Assign the first two to the operation
            Guid operationId = Guid.NewGuid();
            actual = await _extendedQueryTagStore.AssignReindexingOperationAsync(
                expected.Select(x => x.Key).ToList(),
                operationId,
                returnIfCompleted: false);
            AssertEqualTagKeys(expected, actual);

            // Query the tags
            actual = await _extendedQueryTagStore.GetExtendedQueryTagsByOperationAsync(operationId);
            AssertEqualTagKeys(expected, actual);
        }

        [Fact]
        public async Task GivenQueryTags_WhenAssigningReindexingOperation_ThenOnlyReturnDesiredTags()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = DicomTag.PatientAge;
            DicomTag tag3 = DicomTag.PatientMotherBirthName;
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry2 = tag2.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry3 = tag3.BuildAddExtendedQueryTagEntry();

            List<ExtendedQueryTagReference> tags = (await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 }, ready: false))
                .Concat(await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry3 }, ready: true))
                .ToList();

            // Only return tags that are being indexed
            IReadOnlyList<ExtendedQueryTagStoreEntry> actual = await _extendedQueryTagStore.AssignReindexingOperationAsync(
                tags.Select(x => x.Key).ToList(),
                Guid.NewGuid(),
                returnIfCompleted: false);

            AssertEqualTagKeys(tags.Take(2), actual);
        }

        [Fact]
        public async Task GivenCompletedQueryTags_WhenAssigningReindexingOperation_ThenOnlyReturnDesiredTags()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = DicomTag.PatientAge;
            DicomTag tag3 = DicomTag.PatientMotherBirthName;
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry2 = tag2.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry3 = tag3.BuildAddExtendedQueryTagEntry();

            List<ExtendedQueryTagReference> tags = (await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 }, ready: false))
                .Concat(await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry3 }, ready: true))
                .ToList();

            // Only return tags that are being indexed
            IReadOnlyList<ExtendedQueryTagStoreEntry> actual = await _extendedQueryTagStore.AssignReindexingOperationAsync(
                tags.Select(x => x.Key).ToList(),
                Guid.NewGuid(),
                returnIfCompleted: true);

            AssertEqualTagKeys(tags, actual);
        }

        [Fact]
        public async Task GivenQueryTags_WhenCompletingReindexing_ThenOnlyReturnNewlyCompletedTags()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = DicomTag.PatientAge;
            DicomTag tag3 = DicomTag.PatientMotherBirthName;
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry2 = tag2.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry3 = tag3.BuildAddExtendedQueryTagEntry();

            List<ExtendedQueryTagReference> tags = (await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 }, ready: false))
                .Concat(await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry3 }, ready: true))
                .ToList();

            List<int> expectedKeys = tags.Take(2).Select(x => x.Key).ToList();
            IReadOnlyList<ExtendedQueryTagStoreEntry> actual = await _extendedQueryTagStore.AssignReindexingOperationAsync(
                expectedKeys,
                Guid.NewGuid());

            AssertEqualTagKeys(tags.Take(2), actual);
            Assert.True((await _extendedQueryTagStore.CompleteReindexingAsync(expectedKeys)).SequenceEqual(expectedKeys));
        }

        private async Task VerifyTagIsAdded(int key, AddExtendedQueryTagEntry extendedQueryTagEntry, ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Ready)
        {
            var actualExtendedQueryTagEntries = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(extendedQueryTagEntry.Path);
            ExtendedQueryTagStoreEntry actualExtendedQueryTagEntry = actualExtendedQueryTagEntries.First();
            Assert.Equal(key, actualExtendedQueryTagEntry.Key);
            Assert.Equal(extendedQueryTagEntry.Path, actualExtendedQueryTagEntry.Path);
            Assert.Equal(extendedQueryTagEntry.PrivateCreator, actualExtendedQueryTagEntry.PrivateCreator);
            Assert.Equal(extendedQueryTagEntry.VR, actualExtendedQueryTagEntry.VR);
            Assert.Equal(extendedQueryTagEntry.Level, actualExtendedQueryTagEntry.Level.ToString());
            Assert.Equal(status, actualExtendedQueryTagEntry.Status); // Typically we'll set the status to Adding
        }

        private async Task VerifyTagNotExist(string tagPath)
        {
            var extendedQueryTagEntries = await _extendedQueryTagStore.GetExtendedQueryTagsAsync();
            Assert.DoesNotContain(extendedQueryTagEntries, item => item.Path.Equals(tagPath));
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _extendedQueryTagStoreTestHelper.ClearExtendedQueryTagTablesAsync();
        }

        private Task<IReadOnlyList<ExtendedQueryTagReference>> AddExtendedQueryTagsAsync(
            IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries,
            int maxAllowedCount = 128,
            bool ready = true,
            CancellationToken cancellationToken = default)
        {
            return _extendedQueryTagStore.AddExtendedQueryTagsAsync(extendedQueryTagEntries, maxAllowedCount, ready: ready, cancellationToken: cancellationToken);
        }

        private void AssertEqualTagKeys(IEnumerable<ExtendedQueryTagReference> expected, IEnumerable<ExtendedQueryTagStoreEntry> actual)
            => Assert.True(actual.Select(x => x.Key).SequenceEqual(expected.Select(x => x.Key)));
    }
}
