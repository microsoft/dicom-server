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
using Microsoft.Health.Dicom.Core.Features.Common;
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
        private readonly IStoreFactory<IExtendedQueryTagStore> _extendedQueryTagStoreFactory;
        private readonly IStoreFactory<IIndexDataStore> _indexDataStoreFactory;
        private readonly IIndexDataStoreTestHelper _testHelper;

        public ExtendedQueryTagStoreTests(SqlDataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            EnsureArg.IsNotNull(fixture.ExtendedQueryTagStoreFactory, nameof(fixture.ExtendedQueryTagStoreFactory));
            EnsureArg.IsNotNull(fixture.IndexDataStoreFactory, nameof(fixture.IndexDataStoreFactory));
            EnsureArg.IsNotNull(fixture.TestHelper, nameof(fixture.TestHelper));
            _extendedQueryTagStoreFactory = fixture.ExtendedQueryTagStoreFactory;
            _indexDataStoreFactory = fixture.IndexDataStoreFactory;
            _testHelper = fixture.TestHelper;
        }

        [Fact]
        public async Task GivenValidExtendedQueryTags_WhenAddExtendedQueryTag_ThenTagShouldBeAdded()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = new DicomTag(0x0405, 0x1001, "PrivateCreator1");
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry2 = tag2.BuildAddExtendedQueryTagEntry(vr: DicomVRCode.CS);
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            IReadOnlyList<int> keys = await AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 });

            await VerifyTagIsAdded(keys[0], extendedQueryTagEntry1);
            await VerifyTagIsAdded(keys[1], extendedQueryTagEntry2);
        }

        [Fact]
        public async Task GivenUnfinishedExistingExtendedQueryTag_WhenAddExtendedQueryTag_ThenTagShouldBeAdded()
        {
            DicomTag tag = DicomTag.PatientAge;
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();

            // Add and verify the tag was added
            int oldKey = (await AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }, ready: false)).Single();
            await VerifyTagIsAdded(oldKey, extendedQueryTagEntry, ExtendedQueryTagStatus.Adding);

            // Add the tag again before it can be associated with a re-indexing operation
            int newKey = (await AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }, ready: false)).Single();
            await VerifyTagIsAdded(newKey, extendedQueryTagEntry, ExtendedQueryTagStatus.Adding);
            Assert.NotEqual(oldKey, newKey);
        }

        [Fact]
        public async Task GivenCompletedExtendedQueryTag_WhenAddExtendedQueryTag_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            await AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry });
            await Assert.ThrowsAsync<ExtendedQueryTagsAlreadyExistsException>(() => AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }));
        }

        [Fact]
        public async Task GivenReindexingExtendedQueryTag_WhenAddExtendedQueryTag_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            int key = (await AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }, ready: false)).Single();
            Assert.NotEmpty(await extendedQueryTagStore.ConfirmReindexingAsync(new int[] { key }, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ExtendedQueryTagsAlreadyExistsException>(() => AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }));
        }

        [Fact]
        public async Task GivenMoreThanAllowedExtendedQueryTags_WhenAddExtendedQueryTag_ThenShouldThrowException()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            await AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1 });
            DicomTag tag2 = DicomTag.DeviceDescription;
            AddExtendedQueryTagEntry extendedQueryTagEntry2 = tag2.BuildAddExtendedQueryTagEntry();
            await Assert.ThrowsAsync<ExtendedQueryTagsExceedsMaxAllowedCountException>(() => AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry2 }, maxAllowedCount: 1));
        }

        [Fact]
        public async Task GivenExistingExtendedQueryTag_WhenDeleteExtendedQueryTag_ThenTagShouldBeRemoved()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            await AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry });
            await extendedQueryTagStore.DeleteExtendedQueryTagAsync(extendedQueryTagEntry.Path, extendedQueryTagEntry.VR);
            await VerifyTagNotExist(extendedQueryTagStore, extendedQueryTagEntry.Path);
        }

        [Fact]
        public async Task GivenNonExistingExtendedQueryTag_WhenDeleteExtendedQueryTag_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            GetExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildGetExtendedQueryTagEntry();
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            await Assert.ThrowsAsync<ExtendedQueryTagNotFoundException>(() => extendedQueryTagStore.DeleteExtendedQueryTagAsync(extendedQueryTagEntry.Path, extendedQueryTagEntry.VR));
            await VerifyTagNotExist(extendedQueryTagStore, extendedQueryTagEntry.Path);
        }

        [Fact]
        public async Task GivenExistingExtendedQueryTagIndexData_WhenDeleteExtendedQueryTag_ThenShouldDeleteIndexData()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();

            // Prepare index data
            DicomDataset dataset = Samples.CreateRandomInstanceDataset();
            dataset.Add(tag, "123");

            await AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { tag.BuildAddExtendedQueryTagEntry() });
            ExtendedQueryTagStoreEntry storeEntry = (await extendedQueryTagStore.GetExtendedQueryTagsAsync(path: tag.GetPath()))[0];
            QueryTag queryTag = new QueryTag(storeEntry);
            IIndexDataStore indexDataStore = await _indexDataStoreFactory.GetInstanceAsync();
            await indexDataStore.CreateInstanceIndexAsync(dataset, new QueryTag[] { queryTag });
            var extendedQueryTagIndexData = await _testHelper.GetExtendedQueryTagDataForTagKeyAsync(ExtendedQueryTagDataType.StringData, storeEntry.Key);
            Assert.NotEmpty(extendedQueryTagIndexData);

            // Delete tag
            await extendedQueryTagStore.DeleteExtendedQueryTagAsync(storeEntry.Path, storeEntry.VR);
            await VerifyTagNotExist(extendedQueryTagStore, storeEntry.Path);

            // Verify index data is removed
            extendedQueryTagIndexData = await _testHelper.GetExtendedQueryTagDataForTagKeyAsync(ExtendedQueryTagDataType.StringData, storeEntry.Key);
            Assert.Empty(extendedQueryTagIndexData);
        }

        [Fact]
        public async Task GivenQueryTags_WhenConfirmingReindexing_ThenOnlyReturnUnclaimedTags()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = DicomTag.PatientAge;
            DicomTag tag3 = DicomTag.PatientMotherBirthName;
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry2 = tag2.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry3 = tag3.BuildAddExtendedQueryTagEntry();
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();

            List<int> keys = (await AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 }, ready: false))
                .Concat(await AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry3 }, ready: true))
                .ToList();

            IReadOnlyList<ExtendedQueryTagStoreEntry> actual = await extendedQueryTagStore.ConfirmReindexingAsync(keys, Guid.NewGuid().ToString());
            Assert.True(actual.Select(x => x.Key).SequenceEqual(keys.Take(2)));
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
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();

            List<int> keys = (await AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 }, ready: false))
                .Concat(await AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry3 }, ready: true))
                .ToList();

            List<int> expectedKeys = keys.Take(2).ToList();
            IReadOnlyList<ExtendedQueryTagStoreEntry> actual = await extendedQueryTagStore.ConfirmReindexingAsync(keys, Guid.NewGuid().ToString());
            Assert.True(actual.Select(x => x.Key).SequenceEqual(expectedKeys));
            Assert.True((await extendedQueryTagStore.CompleteReindexingAsync(expectedKeys)).SequenceEqual(expectedKeys));
        }

        private async Task VerifyTagIsAdded(int key, AddExtendedQueryTagEntry extendedQueryTagEntry, ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Ready)
        {
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            var actualExtendedQueryTagEntries = await extendedQueryTagStore.GetExtendedQueryTagsAsync(extendedQueryTagEntry.Path);
            ExtendedQueryTagStoreEntry actualExtendedQueryTagEntry = actualExtendedQueryTagEntries.First();
            Assert.Equal(key, actualExtendedQueryTagEntry.Key);
            Assert.Equal(extendedQueryTagEntry.Path, actualExtendedQueryTagEntry.Path);
            Assert.Equal(extendedQueryTagEntry.PrivateCreator, actualExtendedQueryTagEntry.PrivateCreator);
            Assert.Equal(extendedQueryTagEntry.VR, actualExtendedQueryTagEntry.VR);
            Assert.Equal(extendedQueryTagEntry.Level, actualExtendedQueryTagEntry.Level.ToString());
            Assert.Equal(status, actualExtendedQueryTagEntry.Status); // Typically we'll set the status to Adding
        }

        private async Task VerifyTagNotExist(IExtendedQueryTagStore extendedQueryTagStore, string tagPath)
        {
            var extendedQueryTagEntries = await extendedQueryTagStore.GetExtendedQueryTagsAsync();
            Assert.DoesNotContain(extendedQueryTagEntries, item => item.Path.Equals(tagPath));
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            await CleanupTagsAsync(extendedQueryTagStore);
        }

        private async Task CleanupTagsAsync(IExtendedQueryTagStore extendedQueryTagStore)
        {
            var tags = await extendedQueryTagStore.GetExtendedQueryTagsAsync();

            var pendingTags = tags
                .Where(x => x.Status == ExtendedQueryTagStatus.Adding)
                .Select(x => x.Key)
                .ToList();

            if (pendingTags.Count > 0)
            {
                // Pretend that any pending tags that do not have an associated operation are being indexed.
                // Afterwards, "complete" the re-indexing for all the tags such that they can be deleted.
                await extendedQueryTagStore.ConfirmReindexingAsync(pendingTags, Guid.NewGuid().ToString());
                await extendedQueryTagStore.CompleteReindexingAsync(pendingTags);
            }

            foreach (var tag in tags)
            {
                await extendedQueryTagStore.DeleteExtendedQueryTagAsync(tag.Path, tag.VR);
            }
        }

        private Task<IReadOnlyList<int>> AddExtendedQueryTagsAsync(
            IExtendedQueryTagStore extendedQueryTagStore,
            IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries,
            int maxAllowedCount = 128,
            bool ready = true,
            CancellationToken cancellationToken = default)
        {
            return extendedQueryTagStore.AddExtendedQueryTagsAsync(extendedQueryTagEntries, maxAllowedCount, ready: ready, cancellationToken: cancellationToken);
        }
    }
}
