// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;
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
            CustomTagStoreEntry tag1 = CreateCustomTagEntry(1, "45456767", DicomVRCode.AE.ToString(), CustomTagLevel.Instance, CustomTagStatus.Added);
            CustomTagStoreEntry tag2 = CreateCustomTagEntry(2, "01012323", DicomVRCode.FL.ToString(), CustomTagLevel.Series, CustomTagStatus.Reindexing);

            List<CustomTagStoreEntry> storedEntries = new List<CustomTagStoreEntry>() { tag1, tag2 };

            _customTagStore.GetCustomTagsAsync(default).Returns(storedEntries);
            GetAllCustomTagsResponse response = await _getCustomTagsService.GetAllCustomTagsAsync();

            List<CustomTagEntry> result = storedEntries.Select(x => new CustomTagEntry(x)).Except(response.CustomTags).ToList();

            Assert.Empty(result);
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

            Assert.Equal(new CustomTagEntry(stored), response.CustomTag);
        }

        private static CustomTagStoreEntry CreateCustomTagEntry(int key, string path, string vr, CustomTagLevel level = CustomTagLevel.Instance, CustomTagStatus status = CustomTagStatus.Added)
        {
            return new CustomTagStoreEntry(key, path, vr, level, status);
        }
    }
}
