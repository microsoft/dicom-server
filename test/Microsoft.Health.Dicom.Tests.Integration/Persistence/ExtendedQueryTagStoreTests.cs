// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
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
    public class ExtendedQueryTagStoreTests : IClassFixture<SqlDataStoreTestsFixture>
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IIndexDataStore _indexDataStore;
        private readonly IIndexDataStoreTestHelper _testHelper;

        public ExtendedQueryTagStoreTests(SqlDataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            EnsureArg.IsNotNull(fixture.ExtendedQueryTagStore, nameof(fixture.ExtendedQueryTagStore));
            EnsureArg.IsNotNull(fixture.IndexDataStore, nameof(fixture.IndexDataStore));
            EnsureArg.IsNotNull(fixture.TestHelper, nameof(fixture.TestHelper));
            _extendedQueryTagStore = fixture.ExtendedQueryTagStore;
            _indexDataStore = fixture.IndexDataStore;
            _testHelper = fixture.TestHelper;
        }

        [Fact]
        public async Task GivenValidExtendedQueryTags_WhenAddExtendedQueryTag_ThenTagShouldBeAdded()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = new DicomTag(0x0405, 0x1001, "PrivateCreator1");
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry2 = tag2.BuildAddExtendedQueryTagEntry(vr: DicomVRCode.CS);
            await _extendedQueryTagStore.AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 });

            try
            {
                await VerifyTagIsAdded(extendedQueryTagEntry1);
                await VerifyTagIsAdded(extendedQueryTagEntry2);
            }
            finally
            {
                // Delete extended query tag
                await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(extendedQueryTagEntry1.Path, extendedQueryTagEntry1.VR);
                await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(extendedQueryTagEntry2.Path, extendedQueryTagEntry2.VR);
            }
        }

        [Fact]
        public async Task GivenExistingExtendedQueryTag_WhenAddExtendedQueryTag_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();
            await _extendedQueryTagStore.AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry });
            try
            {
                await Assert.ThrowsAsync<ExtendedQueryTagsAlreadyExistsException>(() => _extendedQueryTagStore.AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }));
            }
            finally
            {
                await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(extendedQueryTagEntry.Path, extendedQueryTagEntry.VR);
            }
        }

        [Fact]
        public async Task GivenExistingExtendedQueryTag_WhenDeleteExtendedQueryTag_ThenTagShouldBeRemoved()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();
            await _extendedQueryTagStore.AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry });
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

            await _extendedQueryTagStore.AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { tag.BuildAddExtendedQueryTagEntry() });
            ExtendedQueryTagStoreEntry storeEntry = (await _extendedQueryTagStore.GetExtendedQueryTagsAsync(path: tag.GetPath()))[0];
            QueryTag queryTag = new QueryTag(storeEntry);
            await _indexDataStore.CreateInstanceIndexAsync(dataset, new QueryTag[] { queryTag }, "");
            var extendedQueryTagIndexData = await _testHelper.GetExtendedQueryTagDataForTagKeyAsync(ExtendedQueryTagDataType.StringData, storeEntry.Key);
            Assert.NotEmpty(extendedQueryTagIndexData);

            // Delete tag
            await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(storeEntry.Path, storeEntry.VR);
            await VerifyTagNotExist(storeEntry.Path);

            // Verify index data is removed
            extendedQueryTagIndexData = await _testHelper.GetExtendedQueryTagDataForTagKeyAsync(ExtendedQueryTagDataType.StringData, storeEntry.Key);
            Assert.Empty(extendedQueryTagIndexData);
        }

        private async Task VerifyTagIsAdded(AddExtendedQueryTagEntry extendedQueryTagEntry)
        {
            var actualExtendedQueryTagEntries = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(extendedQueryTagEntry.Path);
            ExtendedQueryTagStoreEntry actualExtendedQueryTagEntry = actualExtendedQueryTagEntries.First();
            Assert.Equal(extendedQueryTagEntry.Path, actualExtendedQueryTagEntry.Path);
            Assert.Equal(extendedQueryTagEntry.PrivateCreator, actualExtendedQueryTagEntry.PrivateCreator);
            Assert.Equal(extendedQueryTagEntry.VR, actualExtendedQueryTagEntry.VR);
            Assert.Equal(extendedQueryTagEntry.Level, actualExtendedQueryTagEntry.Level.ToString());
            Assert.Equal(ExtendedQueryTagStatus.Ready, actualExtendedQueryTagEntry.Status);
        }

        private async Task VerifyTagNotExist(string tagPath)
        {
            var extendedQueryTagEntries = await _extendedQueryTagStore.GetExtendedQueryTagsAsync();
            Assert.DoesNotContain(extendedQueryTagEntries, item => item.Path.Equals(tagPath));
        }
    }
}
