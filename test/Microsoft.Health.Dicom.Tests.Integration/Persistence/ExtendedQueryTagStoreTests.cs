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
        public async Task GivenValidExtendedQueryTags_WhenGettingExtendedQueryTagsByKey_ThenOnlyPresentTagsAreReturned()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = new DicomTag(0x0405, 0x1001, "PrivateCreator1");
            AddExtendedQueryTagEntry expected1 = tag1.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry expected2 = tag2.BuildAddExtendedQueryTagEntry(vr: DicomVRCode.CS);
            IReadOnlyList<int> keys = await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { expected1, expected2 });

            // Fetch the newly added keys (and pass 1 more key we know doesn't have a corresponding entry)
            IReadOnlyList<ExtendedQueryTagStoreEntry> actual = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(
                keys.Concat(new int[] { keys[^1] + 1 }).ToList());

            Assert.Equal(2, actual.Count);
            AssertTag(keys[0], expected1, actual[0]);
            AssertTag(keys[1], expected2, actual[1]);
        }

        [Fact]
        public async Task GivenValidExtendedQueryTags_WhenAddExtendedQueryTag_ThenTagShouldBeAdded()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = new DicomTag(0x0405, 0x1001, "PrivateCreator1");
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry2 = tag2.BuildAddExtendedQueryTagEntry(vr: DicomVRCode.CS);
            IReadOnlyList<int> keys = await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 });

            await VerifyTagIsAdded(keys[0], extendedQueryTagEntry1);
            await VerifyTagIsAdded(keys[1], extendedQueryTagEntry2);
        }

        [Fact]
        public async Task GivenUnfinishedExistingExtendedQueryTag_WhenAddExtendedQueryTag_ThenTagShouldBeAdded()
        {
            DicomTag tag = DicomTag.PatientAge;
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();

            // Add and verify the tag was added
            int oldKey = (await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }, ready: false)).Single();
            await VerifyTagIsAdded(oldKey, extendedQueryTagEntry, ExtendedQueryTagStatus.Adding);

            // Add the tag again before it can be associated with a re-indexing operation
            int newKey = (await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }, ready: false)).Single();
            await VerifyTagIsAdded(newKey, extendedQueryTagEntry, ExtendedQueryTagStatus.Adding);
            Assert.NotEqual(oldKey, newKey);
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
            int key = (await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }, ready: false)).Single();
            Assert.NotEmpty(await _extendedQueryTagStore.AssignReindexingOperationAsync(new int[] { key }, Guid.NewGuid()));
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
            ExtendedQueryTagStoreEntry storeEntry = await _extendedQueryTagStore.GetExtendedQueryTagAsync(tag.GetPath());
            QueryTag queryTag = new QueryTag(storeEntry);
            long watermark = await _indexDataStore.BeginCreateInstanceIndexAsync(dataset, new QueryTag[] { queryTag });
            await _indexDataStore.EndCreateInstanceIndexAsync(dataset, watermark, new QueryTag[] { queryTag });
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
            List<int> expected = (await AddExtendedQueryTagsAsync(
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
                expected,
                operationId,
                returnIfCompleted: false);
            Assert.True(actual.Select(x => x.Key).SequenceEqual(expected));

            // Query the tags
            actual = await _extendedQueryTagStore.GetExtendedQueryTagsByOperationAsync(operationId);
            Assert.True(actual.Select(x => x.Key).SequenceEqual(expected));
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

            List<int> keys = (await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 }, ready: false))
                .Concat(await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry3 }, ready: true))
                .ToList();

            // Only return tags that are being indexed
            IReadOnlyList<ExtendedQueryTagStoreEntry> actual = await _extendedQueryTagStore.AssignReindexingOperationAsync(keys, Guid.NewGuid(), returnIfCompleted: false);
            Assert.True(actual.Select(x => x.Key).SequenceEqual(keys.Take(2)));
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

            List<int> keys = (await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 }, ready: false))
                .Concat(await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry3 }, ready: true))
                .ToList();

            // Only return tags that are being indexed
            IReadOnlyList<ExtendedQueryTagStoreEntry> actual = await _extendedQueryTagStore.AssignReindexingOperationAsync(
                keys,
                Guid.NewGuid(),
                returnIfCompleted: true);

            Assert.True(actual.Select(x => x.Key).SequenceEqual(keys));
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

            List<int> keys = (await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 }, ready: false))
                .Concat(await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry3 }, ready: true))
                .ToList();

            List<int> expectedKeys = keys.Take(2).ToList();
            IReadOnlyList<ExtendedQueryTagStoreEntry> actual = await _extendedQueryTagStore.AssignReindexingOperationAsync(keys, Guid.NewGuid());
            Assert.True(actual.Select(x => x.Key).SequenceEqual(expectedKeys));
            Assert.True((await _extendedQueryTagStore.CompleteReindexingAsync(expectedKeys)).SequenceEqual(expectedKeys));
        }

        [Fact]
        public async Task GivenNotExistingTag_WhenUpdateTagQueryStatus_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            await Assert.ThrowsAsync<ExtendedQueryTagNotFoundException>(() => _extendedQueryTagStore.UpdateQueryStatusAsync(tag.GetPath(), QueryStatus.Enabled));
        }

        [Fact]
        public async Task GivenValidTag_WhenUpdateTagQueryStatus_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            var addEntry = tag.BuildAddExtendedQueryTagEntry();
            await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { addEntry }, ready: true);
            var returnTagStore = await _extendedQueryTagStore.UpdateQueryStatusAsync(tag.GetPath(), QueryStatus.Disabled);
            Assert.Equal(addEntry.Path, returnTagStore.Path);
            Assert.Equal(addEntry.Level, returnTagStore.Level.ToString());
            Assert.Equal(addEntry.PrivateCreator, returnTagStore.PrivateCreator);
            Assert.Equal(addEntry.VR, returnTagStore.VR);
            Assert.Equal(ExtendedQueryTagStatus.Ready, returnTagStore.Status);
            Assert.Equal(QueryStatus.Disabled, returnTagStore.QueryStatus);

            returnTagStore = await _extendedQueryTagStore.UpdateQueryStatusAsync(tag.GetPath(), QueryStatus.Enabled);
            Assert.Equal(QueryStatus.Enabled, returnTagStore.QueryStatus);
        }

        [Fact]
        public async Task GivenValidExtendedQueryTag_WhenAddExtendedQueryTag_ThenTagQueryStatusShouldBeEnabled()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();
            await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry });
            var tagEntry = await _extendedQueryTagStore.GetExtendedQueryTagAsync(tag.GetPath());
            Assert.Equal(QueryStatus.Enabled, tagEntry.QueryStatus);
        }

        [Fact]
        public async Task GivenValidTag_WhenGetTag_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            var addEntry = tag.BuildAddExtendedQueryTagEntry();
            await AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { addEntry }, ready: true);
            var returnTagStore = await _extendedQueryTagStore.GetExtendedQueryTagAsync(tag.GetPath());
            Assert.Equal(addEntry.Path, returnTagStore.Path);
            Assert.Equal(addEntry.Level, returnTagStore.Level.ToString());
            Assert.Equal(addEntry.PrivateCreator, returnTagStore.PrivateCreator);
            Assert.Equal(addEntry.VR, returnTagStore.VR);
            Assert.Equal(ExtendedQueryTagStatus.Ready, returnTagStore.Status);
            Assert.Equal(QueryStatus.Enabled, returnTagStore.QueryStatus);
        }

        [Fact]
        public async Task GivenValidTags_WhenGetTagWithPagination_ThenShouldSucceed()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = DicomTag.DeviceID;
            DicomTag tag3 = DicomTag.PatientAge;
            var addEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            var addEntry2 = tag2.BuildAddExtendedQueryTagEntry();
            var addEntry3 = tag3.BuildAddExtendedQueryTagEntry();
            var addEntries = new AddExtendedQueryTagEntry[] { addEntry1, addEntry2, addEntry3 };
            await AddExtendedQueryTagsAsync(addEntries, ready: true);
            int offset = 0;
            int limit = 1;
            List<ExtendedQueryTagStoreEntry> entries = new List<ExtendedQueryTagStoreEntry>();
            do
            {
                var tagEntries = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(limit, offset);
                if (tagEntries.Count == 0)
                {
                    break;
                }
                else
                {
                    entries.AddRange(tagEntries);
                    offset += limit;
                }

            }
            while (true);

            Assert.Equal(3, entries.Count);
            for (int i = 0; i < addEntries.Length; i++)
            {
                Assert.Equal(addEntries[i].Path, entries[i].Path);
                Assert.Equal(addEntries[i].Level, entries[i].Level.ToString());
                Assert.Equal(addEntries[i].PrivateCreator, entries[i].PrivateCreator);
                Assert.Equal(addEntries[i].VR, entries[i].VR);
                Assert.Equal(ExtendedQueryTagStatus.Ready, entries[i].Status);
                Assert.Equal(QueryStatus.Enabled, entries[i].QueryStatus);
            }
        }

        private async Task VerifyTagIsAdded(int key, AddExtendedQueryTagEntry extendedQueryTagEntry, ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Ready)
        {
            ExtendedQueryTagStoreEntry actual = await _extendedQueryTagStore.GetExtendedQueryTagAsync(extendedQueryTagEntry.Path);
            AssertTag(key, extendedQueryTagEntry, actual, status);
        }

        private Task VerifyTagNotExist(string tagPath)
            => Assert.ThrowsAsync<ExtendedQueryTagNotFoundException>(() => _extendedQueryTagStore.GetExtendedQueryTagAsync(tagPath));

        private void AssertTag(int key, AddExtendedQueryTagEntry expected, ExtendedQueryTagStoreEntry actual, ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Ready)
        {
            Assert.Equal(key, actual.Key);
            Assert.Equal(expected.Path, actual.Path);
            Assert.Equal(expected.PrivateCreator, actual.PrivateCreator);
            Assert.Equal(expected.VR, actual.VR);
            Assert.Equal(expected.Level, actual.Level.ToString());
            Assert.Equal(status, actual.Status); // Typically we'll set the status to Adding
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _extendedQueryTagStoreTestHelper.ClearExtendedQueryTagTablesAsync();
        }

        private async Task<IReadOnlyList<int>> AddExtendedQueryTagsAsync(
            IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries,
            int maxAllowedCount = 128,
            bool ready = true,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ExtendedQueryTagStoreEntry> tags = await _extendedQueryTagStore.AddExtendedQueryTagsAsync(
                extendedQueryTagEntries,
                maxAllowedCount,
                ready: ready,
                cancellationToken: cancellationToken);

            return tags.Select(x => x.Key).ToList();
        }
    }
}
