// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed
{
    public class DeleteExtendedQueryTagServiceTests
    {
        private IExtendedQueryTagStore _extendedQueryTagStore;
        private IDeleteExtendedQueryTagService _extendedQueryTagService;

        public DeleteExtendedQueryTagServiceTests()
        {
            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            FeatureConfiguration featureConfiguration = new FeatureConfiguration() { EnableExtendedQueryTags = true };
            _extendedQueryTagService = new DeleteExtendedQueryTagService(_extendedQueryTagStore, new DicomTagParser(), featureConfiguration);
        }

        [Fact]
        public async Task GivenFeatureDisabled_WhenDeleteExtendedQueryTagIsInvoked_ThenShouldThrowException()
        {
            FeatureConfiguration featureConfiguration = new FeatureConfiguration() { EnableExtendedQueryTags = false };
            IDeleteExtendedQueryTagService extendedQueryTagService = new DeleteExtendedQueryTagService(_extendedQueryTagStore, new DicomTagParser(), featureConfiguration);

            DicomTag tag = DicomTag.DeviceSerialNumber;
            ExtendedQueryTagEntry entry = tag.BuildExtendedQueryTagEntry();
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => extendedQueryTagService.DeleteExtendedQueryTagAsync("0000000A"));
        }

        [Fact]
        public async Task GivenInputTagPath_WhenDeleteExtendedQueryTagIsInvoked_ThenShouldThrowException()
        {
            await Assert.ThrowsAsync<InvalidExtendedQueryTagPathException>(() => _extendedQueryTagService.DeleteExtendedQueryTagAsync("0000000A"));
        }

        [Fact]
        public async Task GivenNotExistingTagPath_WhenDeleteExtendedQueryTagIsInvoked_ThenShouldThrowException()
        {
            _extendedQueryTagStore.GetExtendedQueryTagsAsync(default, default).ReturnsForAnyArgs(new Func<NSubstitute.Core.CallInfo, IReadOnlyList<ExtendedQueryTagStoreEntry>>((x) => { throw new Exception(); }));
            await Assert.ThrowsAsync<Exception>(() => _extendedQueryTagService.DeleteExtendedQueryTagAsync(DicomTag.DeviceSerialNumber.GetPath()));
        }

        [Fact]
        public async Task GivenValidTagPath_WhenDeleteExtendedQueryTagIsInvoked_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            string tagPath = tag.GetPath();
            ExtendedQueryTagStoreEntry entry = tag.BuildExtendedQueryTagStoreEntry();
            _extendedQueryTagStore.GetExtendedQueryTagsAsync(default, default).ReturnsForAnyArgs(new List<ExtendedQueryTagStoreEntry> { entry });
            await _extendedQueryTagService.DeleteExtendedQueryTagAsync(tagPath);
            await _extendedQueryTagStore.ReceivedWithAnyArgs(1)
                .DeleteExtendedQueryTagAsync(default, default);
        }
    }
}
