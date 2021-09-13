// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom;
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
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IDicomTagParser _dicomTagParser;
        private readonly IGetExtendedQueryTagsService _getExtendedQueryTagsService;

        public GetExtendedQueryTagsServiceTests()
        {
            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _dicomTagParser = Substitute.For<IDicomTagParser>();
            _getExtendedQueryTagsService = new GetExtendedQueryTagsService(_extendedQueryTagStore, _dicomTagParser);
        }

        [Fact]
        public async Task GivenRequestForMultipleTags_WhenNoTagsAreStored_ThenReturnEmptyResult()
        {
            _extendedQueryTagStore.GetExtendedQueryTagsAsync(7, 0).Returns(Array.Empty<ExtendedQueryTagStoreEntry>());
            GetExtendedQueryTagsResponse response = await _getExtendedQueryTagsService.GetExtendedQueryTagsAsync(7, 0);
            await _extendedQueryTagStore.Received(1).GetExtendedQueryTagsAsync(7, 0);

            Assert.Empty(response.ExtendedQueryTags);
        }

        [Fact]
        public async Task GivenRequestForMultipleTags_WhenMultipleTagsAreStored_ThenExtendedQueryTagEntryListShouldBeReturned()
        {
            ExtendedQueryTagStoreEntry tag1 = CreateExtendedQueryTagEntry(1, "45456767", DicomVRCode.AE.ToString(), null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready);
            ExtendedQueryTagStoreEntry tag2 = CreateExtendedQueryTagEntry(2, "04051001", DicomVRCode.FL.ToString(), "PrivateCreator1", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding);

            List<ExtendedQueryTagStoreEntry> storedEntries = new List<ExtendedQueryTagStoreEntry>() { tag1, tag2 };

            _extendedQueryTagStore.GetExtendedQueryTagsAsync(101, 303).Returns(storedEntries);
            GetExtendedQueryTagsResponse response = await _getExtendedQueryTagsService.GetExtendedQueryTagsAsync(101, 303);
            await _extendedQueryTagStore.Received(1).GetExtendedQueryTagsAsync(101, 303);

            var expected = new GetExtendedQueryTagEntry[] { tag1.ToExtendedQueryTagEntry(), tag2.ToExtendedQueryTagEntry() };
            Assert.Equal(expected, response.ExtendedQueryTags, ExtendedQueryTagEntryEqualityComparer.Default);
        }

        [Theory]
        [InlineData("00181003")]
        [InlineData("DeviceID")]
        public async Task GivenRequestForExtendedQueryTag_WhenTagDoesntExist_ThenExceptionShouldBeThrown(string tagPath)
        {
            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            string actualTagPath = parsedTags[0].GetPath();
            _extendedQueryTagStore
                .GetExtendedQueryTagAsync(actualTagPath, default)
                .Returns(Task.FromException<ExtendedQueryTagStoreEntry>(new ExtendedQueryTagNotFoundException("Tag doesn't exist")));
            await Assert.ThrowsAsync<ExtendedQueryTagNotFoundException>(() => _getExtendedQueryTagsService.GetExtendedQueryTagAsync(tagPath));
            await _extendedQueryTagStore.Received(1).GetExtendedQueryTagAsync(actualTagPath, default);
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

            _extendedQueryTagStore.GetExtendedQueryTagAsync(tagPath, default).Returns(stored);
            GetExtendedQueryTagResponse response = await _getExtendedQueryTagsService.GetExtendedQueryTagAsync(tagPath);
            await _extendedQueryTagStore.Received(1).GetExtendedQueryTagAsync(tagPath, default);

            Assert.Equal(stored.ToExtendedQueryTagEntry(), response.ExtendedQueryTag, ExtendedQueryTagEntryEqualityComparer.Default);
        }

        private static ExtendedQueryTagStoreEntry CreateExtendedQueryTagEntry(int key, string path, string vr, string privateCreator = null, QueryTagLevel level = QueryTagLevel.Instance, ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Ready)
        {
            return new ExtendedQueryTagStoreEntry(key, path, vr, privateCreator, level, status, QueryStatus.Enabled);
        }
    }
}
