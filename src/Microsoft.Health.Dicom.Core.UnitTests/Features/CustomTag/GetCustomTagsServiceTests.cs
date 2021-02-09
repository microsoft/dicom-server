// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
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
            _customTagStore.GetCustomTagsAsync(default).Returns(new List<CustomTagEntry>());
            var exception = await Assert.ThrowsAsync<CustomTagNotFoundException>(() => _getCustomTagsService.GetAllCustomTagsAsync());

            Assert.Equal("No custom tags can be found.", exception.Message);
        }

        [Fact]
        public async Task GivenRequestForAllTags_WhenMultipleTagsAreStored_ThenCustomTagEntryListShouldBeReturned()
        {
            CustomTagEntry tag1 = CreateCustomTagEntry("45456767", DicomVRCode.AE.ToString(), CustomTagLevel.Instance, CustomTagStatus.Added);
            CustomTagEntry tag2 = CreateCustomTagEntry("01012323", DicomVRCode.FL.ToString(), CustomTagLevel.Series, CustomTagStatus.Reindexing);

            List<CustomTagEntry> storedEntries = new List<CustomTagEntry>() { tag1, tag2 };

            _customTagStore.GetCustomTagsAsync(default).Returns(storedEntries);
            GetAllCustomTagsResponse response = await _getCustomTagsService.GetAllCustomTagsAsync();

            List<CustomTagEntry> result = storedEntries.Except(response.CustomTags).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GivenRequestForCustomTag_WhenTagDoesntExist_ThenExceptionShouldBeThrown()
        {
            string tagPath = "(0101,2323)";
            string storedTagPath = "01012323";

            _dicomTagParser.ParseFormattedTagPath(tagPath).Returns(storedTagPath);
            _customTagStore.GetCustomTagsAsync(storedTagPath, default).Returns(new List<CustomTagEntry>());
            var exception = await Assert.ThrowsAsync<CustomTagNotFoundException>(() => _getCustomTagsService.GetCustomTagAsync(tagPath));

            Assert.Equal(string.Format("The specified custom tag with tag path {0} cannot be found.", tagPath), exception.Message);
        }

        [Fact]
        public async Task GivenRequestForCustomTag_WhenTagExists_ThenCustomTagEntryShouldBeReturned()
        {
            string tagPath = "(0101,2323)";
            string storedTagPath = "01012323";
            CustomTagEntry stored = CreateCustomTagEntry("01012323", DicomVRCode.AE.ToString());

            _dicomTagParser.ParseFormattedTagPath(tagPath).Returns(storedTagPath);
            _customTagStore.GetCustomTagsAsync(storedTagPath, default).Returns(new List<CustomTagEntry> { stored });
            GetCustomTagResponse response = await _getCustomTagsService.GetCustomTagAsync(tagPath);

            Assert.Equal(stored, response.CustomTag);
        }

        private static CustomTagEntry CreateCustomTagEntry(string path, string vr, CustomTagLevel level = CustomTagLevel.Instance, CustomTagStatus status = CustomTagStatus.Added)
        {
            return new CustomTagEntry { Path = path, VR = vr, Level = level, Status = status };
        }
    }
}
