// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using Microsoft.Health.Dicom.Tests.Common.Comparers;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag
{
    public class GetExtendedQueryTagsServiceTests
    {
        private IExtendedQueryTagStore _extendedQueryTagStore;
        private IDicomTagParser _dicomTagParser;
        private IGetExtendedQueryTagsService _getExtendedQueryTagsService;

        public GetExtendedQueryTagsServiceTests()
        {
            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _dicomTagParser = Substitute.For<IDicomTagParser>();
            FeatureConfiguration featureConfiguration = new FeatureConfiguration() { EnableExtendedQueryTags = true };
            _getExtendedQueryTagsService = new GetExtendedQueryTagsService(_extendedQueryTagStore, _dicomTagParser, Options.Create(featureConfiguration));
        }

        [Fact]
        public async Task GivenFeatureDisabled_WhenGetExtendedQueryTagsIsInvoked_ThenShouldThrowException()
        {
            FeatureConfiguration featureConfiguration = new FeatureConfiguration() { EnableExtendedQueryTags = false };
            IGetExtendedQueryTagsService getExtendedQueryTagsService = new GetExtendedQueryTagsService(_extendedQueryTagStore, new DicomTagParser(), Options.Create(featureConfiguration));

            DicomTag tag = DicomTag.DeviceSerialNumber;
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => getExtendedQueryTagsService.GetAllExtendedQueryTagsAsync());
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => getExtendedQueryTagsService.GetExtendedQueryTagAsync(tag.GetPath()));
        }

        [Fact]
        public async Task GivenRequestForAllTags_WhenNoTagsAreStored_ThenExceptionShouldBeThrown()
        {
            _extendedQueryTagStore.GetExtendedQueryTagsAsync(default).Returns(new List<ExtendedQueryTagStoreEntry>());
            GetAllExtendedQueryTagsResponse response = await _getExtendedQueryTagsService.GetAllExtendedQueryTagsAsync();

            Assert.Empty(response.ExtendedQueryTags);
        }

        [Fact]
        public async Task GivenRequestForAllTags_WhenMultipleTagsAreStored_ThenExtendedQueryTagEntryListShouldBeReturned()
        {
            ExtendedQueryTagStoreEntry tag1 = CreateExtendedQueryTagEntry(1, "45456767", DicomVRCode.AE.ToString(), null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready);
            ExtendedQueryTagStoreEntry tag2 = CreateExtendedQueryTagEntry(2, "04051001", DicomVRCode.FL.ToString(), "PrivateCreator1", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding);

            List<ExtendedQueryTagStoreEntry> storedEntries = new List<ExtendedQueryTagStoreEntry>() { tag1, tag2 };

            _extendedQueryTagStore.GetExtendedQueryTagsAsync(default).Returns(storedEntries);
            GetAllExtendedQueryTagsResponse response = await _getExtendedQueryTagsService.GetAllExtendedQueryTagsAsync();

            var expected = new ExtendedQueryTagEntry[] { tag1.ToExtendedQueryTagEntry(), tag2.ToExtendedQueryTagEntry() };

            Assert.Equal(expected, response.ExtendedQueryTags, ExtendedQueryTagEntryEqualityComparer.Default);
        }

        [Fact]
        public async Task GivenRequestForExtendedQueryTag_WhenTagDoesntExist_ThenExceptionShouldBeThrown()
        {
            string tagPath = DicomTag.DeviceID.GetPath();
            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _extendedQueryTagStore.GetExtendedQueryTagsAsync(tagPath, default).Returns(new List<ExtendedQueryTagStoreEntry>());
            var exception = await Assert.ThrowsAsync<ExtendedQueryTagNotFoundException>(() => _getExtendedQueryTagsService.GetExtendedQueryTagAsync(tagPath));

            Assert.Equal(string.Format("The specified extended query tag with tag path {0} cannot be found.", tagPath), exception.Message);
        }

        [Fact]
        public async Task GivenRequestForExtendedQueryTag_WhenTagExists_ThenExtendedQueryTagEntryShouldBeReturned()
        {
            string tagPath = DicomTag.DeviceID.GetPath();
            ExtendedQueryTagStoreEntry stored = CreateExtendedQueryTagEntry(5, tagPath, DicomVRCode.AE.ToString());
            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _extendedQueryTagStore.GetExtendedQueryTagsAsync(tagPath, default).Returns(new List<ExtendedQueryTagStoreEntry> { stored });
            GetExtendedQueryTagResponse response = await _getExtendedQueryTagsService.GetExtendedQueryTagAsync(tagPath);

            Assert.Equal(stored.ToExtendedQueryTagEntry(), response.ExtendedQueryTag, ExtendedQueryTagEntryEqualityComparer.Default);
        }

        private static ExtendedQueryTagStoreEntry CreateExtendedQueryTagEntry(int key, string path, string vr, string privateCreator = null, QueryTagLevel level = QueryTagLevel.Instance, ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Ready)
        {
            return new ExtendedQueryTagStoreEntry(key, path, vr, privateCreator, level, status);
        }
    }
}
