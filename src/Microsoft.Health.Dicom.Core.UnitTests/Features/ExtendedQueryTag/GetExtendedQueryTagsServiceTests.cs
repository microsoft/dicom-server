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
using Microsoft.Health.Dicom.Core.Features.Routing;
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
        private readonly IUrlResolver _urlResolver;
        private readonly IGetExtendedQueryTagsService _getExtendedQueryTagsService;

        public GetExtendedQueryTagsServiceTests()
        {
            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _dicomTagParser = Substitute.For<IDicomTagParser>();
            _urlResolver = Substitute.For<IUrlResolver>();
            _getExtendedQueryTagsService = new GetExtendedQueryTagsService(_extendedQueryTagStore, _dicomTagParser, _urlResolver);
        }

        [Fact]
        public async Task GivenRequestForMultipleTags_WhenNoTagsAreStored_ThenReturnEmptyResult()
        {
            _extendedQueryTagStore.GetExtendedQueryTagsAsync(7, 0).Returns(Array.Empty<ExtendedQueryTagStoreJoinEntry>());
            GetExtendedQueryTagsResponse response = await _getExtendedQueryTagsService.GetExtendedQueryTagsAsync(7, 0);
            await _extendedQueryTagStore.Received(1).GetExtendedQueryTagsAsync(7, 0);
            _urlResolver.DidNotReceiveWithAnyArgs().ResolveQueryTagErrorsUri(default);

            Assert.Empty(response.ExtendedQueryTags);
        }

        [Fact]
        public async Task GivenRequestForMultipleTags_WhenMultipleTagsAreStored_ThenExtendedQueryTagEntryListShouldBeReturned()
        {
            Guid operationId = Guid.NewGuid();
            ExtendedQueryTagStoreJoinEntry tag1 = CreateJoinEntry(1, "45456767", DicomVRCode.AE.ToString(), null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, 0, operationId);
            ExtendedQueryTagStoreJoinEntry tag2 = CreateJoinEntry(2, "04051001", DicomVRCode.FL.ToString(), "PrivateCreator1", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding, 7);
            var operationUrl = new Uri("https://dicom.contoso.io/unit/test/operations/" + operationId.ToString("N"), UriKind.Absolute);
            var tag2Errors = new Uri("https://dicom.contoso.io/unit/test/extendedquerytags/" + tag2.Path + "/errors", UriKind.Absolute);

            var storedEntries = new List<ExtendedQueryTagStoreJoinEntry>() { tag1, tag2 };

            _extendedQueryTagStore.GetExtendedQueryTagsAsync(101, 303).Returns(storedEntries);
            _urlResolver.ResolveOperationStatusUri(operationId).Returns(operationUrl);
            _urlResolver.ResolveQueryTagErrorsUri(tag2.Path).Returns(tag2Errors);
            GetExtendedQueryTagsResponse response = await _getExtendedQueryTagsService.GetExtendedQueryTagsAsync(101, 303);
            await _extendedQueryTagStore.Received(1).GetExtendedQueryTagsAsync(101, 303);

            var expected = new GetExtendedQueryTagEntry[] { tag1.ToGetExtendedQueryTagEntry(_urlResolver), tag2.ToGetExtendedQueryTagEntry(_urlResolver) };
            Assert.Equal(expected, response.ExtendedQueryTags, ExtendedQueryTagEntryEqualityComparer.Default);
            _urlResolver.Received(2).ResolveOperationStatusUri(operationId);
            _urlResolver.Received(2).ResolveQueryTagErrorsUri(tag2.Path);
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
                .Returns(Task.FromException<ExtendedQueryTagStoreJoinEntry>(new ExtendedQueryTagNotFoundException("Tag doesn't exist")));
            await Assert.ThrowsAsync<ExtendedQueryTagNotFoundException>(() => _getExtendedQueryTagsService.GetExtendedQueryTagAsync(tagPath));
            await _extendedQueryTagStore.Received(1).GetExtendedQueryTagAsync(actualTagPath, default);
            _urlResolver.DidNotReceiveWithAnyArgs().ResolveQueryTagErrorsUri(default);
        }

        [Fact]
        public async Task GivenRequestForExtendedQueryTag_WhenTagExists_ThenExtendedQueryTagEntryShouldBeReturned()
        {
            string tagPath = DicomTag.DeviceID.GetPath();
            ExtendedQueryTagStoreJoinEntry stored = CreateJoinEntry(5, tagPath, DicomVRCode.AE.ToString());
            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _extendedQueryTagStore.GetExtendedQueryTagAsync(tagPath, default).Returns(stored);
            GetExtendedQueryTagResponse response = await _getExtendedQueryTagsService.GetExtendedQueryTagAsync(tagPath);
            await _extendedQueryTagStore.Received(1).GetExtendedQueryTagAsync(tagPath, default);
            _urlResolver.DidNotReceiveWithAnyArgs().ResolveQueryTagErrorsUri(default);

            Assert.Equal(stored.ToGetExtendedQueryTagEntry(), response.ExtendedQueryTag, ExtendedQueryTagEntryEqualityComparer.Default);
        }

        private static ExtendedQueryTagStoreJoinEntry CreateJoinEntry(
            int key,
            string path,
            string vr,
            string privateCreator = null,
            QueryTagLevel level = QueryTagLevel.Instance,
            ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Ready,
            int errorCount = 0,
            Guid? operationId = null)
        {
            return new ExtendedQueryTagStoreJoinEntry(key, path, vr, privateCreator, level, status, QueryStatus.Enabled, errorCount, operationId);
        }
    }
}
