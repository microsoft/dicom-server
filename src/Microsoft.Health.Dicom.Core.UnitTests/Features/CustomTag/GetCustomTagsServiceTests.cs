// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;
using Microsoft.Health.Dicom.Tests.Common.Comparers;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag
{
    public class GetCustomTagsServiceTests
    {
        private ICustomTagStore _customTagStore;
        private IDicomTagParser _dicomTagParser;
        private IGetCustomTagsService _getCustomTagsService;

        public GetCustomTagsServiceTests()
        {
            _customTagStore = Substitute.For<ICustomTagStore>();
            _dicomTagParser = Substitute.For<IDicomTagParser>();
            _getCustomTagsService = new GetCustomTagsService(_customTagStore, _dicomTagParser);
        }

        [Fact]
        public async Task GivenRequestForAllTags_WhenNoTagsAreStored_ThenExceptionShouldBeThrown()
        {
            _customTagStore.GetCustomTagsAsync(default).Returns(new List<CustomTagStoreEntry>());
            GetAllCustomTagsResponse response = await _getCustomTagsService.GetAllCustomTagsAsync();

            Assert.Empty(response.CustomTags);
        }

        [Fact]
        public async Task GivenRequestForAllTags_WhenMultipleTagsAreStored_ThenCustomTagEntryListShouldBeReturned()
        {
            CustomTagStoreEntry tag1 = CreateCustomTagEntry(1, "45456767", DicomVRCode.AE.ToString(), null, CustomTagLevel.Instance, CustomTagStatus.Added);
            CustomTagStoreEntry tag2 = CreateCustomTagEntry(2, "04051001", DicomVRCode.FL.ToString(), "PrivateCreator1", CustomTagLevel.Series, CustomTagStatus.Reindexing);

            List<CustomTagStoreEntry> storedEntries = new List<CustomTagStoreEntry>() { tag1, tag2 };

            _customTagStore.GetCustomTagsAsync(default).Returns(storedEntries);
            GetAllCustomTagsResponse response = await _getCustomTagsService.GetAllCustomTagsAsync();

            var expected = new CustomTagEntry[] { tag1.ToCustomTagEntry(), tag2.ToCustomTagEntry() };

            Assert.Equal(expected, response.CustomTags, new CustomTagEntryEqualityComparer());
        }

        [Fact]
        public async Task GivenRequestForCustomTag_WhenTagDoesntExist_ThenExceptionShouldBeThrown()
        {
            string tagPath = DicomTag.DeviceID.GetPath();
            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _customTagStore.GetCustomTagsAsync(tagPath, default).Returns(new List<CustomTagStoreEntry>());
            var exception = await Assert.ThrowsAsync<CustomTagNotFoundException>(() => _getCustomTagsService.GetCustomTagAsync(tagPath));

            Assert.Equal(string.Format("The specified custom tag with tag path {0} cannot be found.", tagPath), exception.Message);
        }

        [Fact]
        public async Task GivenRequestForCustomTag_WhenTagExists_ThenCustomTagEntryShouldBeReturned()
        {
            string tagPath = DicomTag.DeviceID.GetPath();
            CustomTagStoreEntry stored = CreateCustomTagEntry(5, tagPath, DicomVRCode.AE.ToString());
            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _customTagStore.GetCustomTagsAsync(tagPath, default).Returns(new List<CustomTagStoreEntry> { stored });
            GetCustomTagResponse response = await _getCustomTagsService.GetCustomTagAsync(tagPath);

            Assert.Equal(stored.ToCustomTagEntry(), response.CustomTag, new CustomTagEntryEqualityComparer());
        }

        private static CustomTagStoreEntry CreateCustomTagEntry(int key, string path, string vr, string privateCreator = null, CustomTagLevel level = CustomTagLevel.Instance, CustomTagStatus status = CustomTagStatus.Added)
        {
            return new CustomTagStoreEntry(key, path, vr, privateCreator, level, status);
        }
    }
}
