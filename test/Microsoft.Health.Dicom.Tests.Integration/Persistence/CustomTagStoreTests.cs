// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class CustomTagStoreTests : IClassFixture<SqlDataStoreTestsFixture>
    {
        private readonly ICustomTagStore _customTagStore;

        public CustomTagStoreTests(SqlDataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _customTagStore = fixture.CustomTagStore;
        }

        [Fact]
        public async Task GivenValidCustomTags_WhenAddCustomTag_ThenTagShouldBeAdded()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = DicomTag.ApprovalStatusDateTime;
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

        private async Task VerifyTagIsAdded(CustomTagEntry customTagEntry)
        {
            var actualCustomTagEntries = await _customTagStore.GetCustomTagsAsync(customTagEntry.Path);
            CustomTagEntry actualCustomTagEntry = new CustomTagEntry(actualCustomTagEntries.First());
            Assert.Equal(customTagEntry.Path, actualCustomTagEntry.Path);
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
