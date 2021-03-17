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
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.SqlServer.Features.CustomTag;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    /// Tests for CustomTagStore
    /// </summary>
    public class CustomTagStoreTests : IClassFixture<SqlDataStoreTestsFixture>
    {
        private readonly ICustomTagStore _customTagStore;
        private readonly IIndexDataStore _indexDataStore;
        private readonly IIndexDataStoreTestHelper _testHelper;

        public CustomTagStoreTests(SqlDataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            EnsureArg.IsNotNull(fixture.CustomTagStore, nameof(fixture.CustomTagStore));
            EnsureArg.IsNotNull(fixture.IndexDataStore, nameof(fixture.IndexDataStore));
            EnsureArg.IsNotNull(fixture.TestHelper, nameof(fixture.TestHelper));
            _customTagStore = fixture.CustomTagStore;
            _indexDataStore = fixture.IndexDataStore;
            _testHelper = fixture.TestHelper;
        }

        [Fact]
        public async Task GivenValidCustomTags_WhenAddCustomTag_ThenTagShouldBeAdded()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = DicomTag.DateOfSecondaryCapture;
            CustomTagEntry customTagEntry1 = tag1.BuildCustomTagEntry();
            CustomTagEntry customTagEntry2 = tag2.BuildCustomTagEntry();
            await _customTagStore.AddCustomTagsAsync(new CustomTagEntry[] { customTagEntry1, customTagEntry2 });

            try
            {
                await VerifyTagIsAdded(customTagEntry1);
                await VerifyTagIsAdded(customTagEntry2);
            }
            finally
            {
                // Delete custom tag
                await _customTagStore.DeleteCustomTagAsync(customTagEntry1.Path, customTagEntry1.VR);
                await _customTagStore.DeleteCustomTagAsync(customTagEntry2.Path, customTagEntry2.VR);
            }
        }

        [Fact]
        public async Task GivenExistingCustomTag_WhenAddCustomTag_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry customTagEntry = tag.BuildCustomTagEntry();
            await _customTagStore.AddCustomTagsAsync(new CustomTagEntry[] { customTagEntry });
            try
            {
                await Assert.ThrowsAsync<CustomTagsAlreadyExistsException>(() => _customTagStore.AddCustomTagsAsync(new CustomTagEntry[] { customTagEntry }));
            }
            finally
            {
                await _customTagStore.DeleteCustomTagAsync(customTagEntry.Path, customTagEntry.VR);
            }
        }

        [Fact]
        public async Task GivenExistingCustomTag_WhenDeleteCustomTag_ThenTagShouldBeRemoved()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry customTagEntry = tag.BuildCustomTagEntry();
            await _customTagStore.AddCustomTagsAsync(new CustomTagEntry[] { customTagEntry });
            await _customTagStore.DeleteCustomTagAsync(customTagEntry.Path, customTagEntry.VR);
            await VerifyTagNotExist(customTagEntry.Path);
        }

        [Fact]
        public async Task GivenNonExistingCustomTag_WhenDeleteCustomTag_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry customTagEntry = tag.BuildCustomTagEntry();
            await Assert.ThrowsAsync<CustomTagNotFoundException>(() => _customTagStore.DeleteCustomTagAsync(customTagEntry.Path, customTagEntry.VR));
            await VerifyTagNotExist(customTagEntry.Path);
        }

        [Fact]
        public async Task GivenExistingCustomTagIndexData_WhenDeleteCustomTag_ThenShouldDeleteIndexData()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;

            // Prepare index data
            DicomDataset dataset = Samples.CreateRandomInstanceDataset();
            dataset.Add(tag, "123");

            await _customTagStore.AddCustomTagsAsync(new CustomTagEntry[] { tag.BuildCustomTagEntry() });
            CustomTagStoreEntry storeEntry = (await _customTagStore.GetCustomTagsAsync(path: tag.GetPath()))[0];
            IndexTag indexTag = new IndexTag(storeEntry);
            await _indexDataStore.CreateInstanceIndexAsync(dataset, new IndexTag[] { indexTag });
            var customTagIndexData = await _testHelper.GetCustomTagDataForTagKeyAsync(CustomTagDataType.StringData, storeEntry.Key);
            Assert.NotEmpty(customTagIndexData);

            // Delete tag
            await _customTagStore.DeleteCustomTagAsync(storeEntry.Path, storeEntry.VR);
            await VerifyTagNotExist(storeEntry.Path);

            // Verify index data is removed
            customTagIndexData = await _testHelper.GetCustomTagDataForTagKeyAsync(CustomTagDataType.StringData, storeEntry.Key);
            Assert.Empty(customTagIndexData);
        }

        private async Task VerifyTagIsAdded(CustomTagEntry customTagEntry)
        {
            var actualCustomTagEntries = await _customTagStore.GetCustomTagsAsync(customTagEntry.Path);
            CustomTagEntry actualCustomTagEntry = actualCustomTagEntries.First().ToCustomTagEntry();
            Assert.Equal(customTagEntry.Path, actualCustomTagEntry.Path);
            Assert.Equal(customTagEntry.PrivateCreator, actualCustomTagEntry.PrivateCreator);
            Assert.Equal(customTagEntry.VR, actualCustomTagEntry.VR);
            Assert.Equal(customTagEntry.Level, actualCustomTagEntry.Level);
            Assert.Equal(CustomTagStatus.Added, actualCustomTagEntry.Status);
        }

        private async Task VerifyTagNotExist(string tagPath)
        {
            var customTagEntries = await _customTagStore.GetCustomTagsAsync();
            Assert.DoesNotContain(customTagEntries, item => item.Path.Equals(tagPath));
        }
    }
}
