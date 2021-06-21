// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
            await extendedQueryTagStore.AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 }, 128);

            await VerifyTagIsAdded(extendedQueryTagEntry1);
            await VerifyTagIsAdded(extendedQueryTagEntry2);
        }

        [Fact]
        public async Task GivenExistingExtendedQueryTag_WhenAddExtendedQueryTag_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            await AddExtendedQueryTagsAsync(extendedQueryTagStore, new AddExtendedQueryTagEntry[] { extendedQueryTagEntry });
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
            await Assert.ThrowsAsync<ExtendedQueryTagsExceedsMaxAllowedCountException>(() => extendedQueryTagStore.AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry2 }, maxAllowedCount: 1));
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

        private async Task VerifyTagIsAdded(AddExtendedQueryTagEntry extendedQueryTagEntry)
        {
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            var actualExtendedQueryTagEntries = await extendedQueryTagStore.GetExtendedQueryTagsAsync(extendedQueryTagEntry.Path);
            ExtendedQueryTagStoreEntry actualExtendedQueryTagEntry = actualExtendedQueryTagEntries.First();
            Assert.Equal(extendedQueryTagEntry.Path, actualExtendedQueryTagEntry.Path);
            Assert.Equal(extendedQueryTagEntry.PrivateCreator, actualExtendedQueryTagEntry.PrivateCreator);
            Assert.Equal(extendedQueryTagEntry.VR, actualExtendedQueryTagEntry.VR);
            Assert.Equal(extendedQueryTagEntry.Level, actualExtendedQueryTagEntry.Level.ToString());
            Assert.Equal(ExtendedQueryTagStatus.Ready, actualExtendedQueryTagEntry.Status);
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
            foreach (var tag in tags)
            {
                await extendedQueryTagStore.DeleteExtendedQueryTagAsync(tag.Path, tag.VR);
            }
        }


        private Task AddExtendedQueryTagsAsync(IExtendedQueryTagStore extendedQueryTagStore, IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries, int maxAllowedCount = 128, CancellationToken cancellationToken = default)
        {
            return extendedQueryTagStore.AddExtendedQueryTagsAsync(extendedQueryTagEntries, maxAllowedCount, cancellationToken);
        }
    }
}
