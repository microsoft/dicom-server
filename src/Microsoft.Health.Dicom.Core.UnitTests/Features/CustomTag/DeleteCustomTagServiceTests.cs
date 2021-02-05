// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed
{
    public class DeleteCustomTagServiceTests
    {
        private ICustomTagStore _customTagStore;
        private IDeleteCustomTagService _customTagService;

        public DeleteCustomTagServiceTests()
        {
            _customTagStore = Substitute.For<ICustomTagStore>();
            _customTagService = new DeleteCustomTagService(_customTagStore, new DicomTagParser(), NullLogger<DeleteCustomTagService>.Instance);
        }

        [Fact]
        public async Task GivenInputTagPath_WhenDeleteCustomTagIsInvoked_ThenShouldThrowException()
        {
            await Assert.ThrowsAsync<InvalidCustomTagPathException>(() => _customTagService.DeleteCustomTagAsync("0000000A"));
        }

        [Fact]
        public async Task GivenNotExistingTagPath_WhenDeleteCustomTagIsInvoked_ThenShouldThrowException()
        {
            _customTagStore.GetCustomTagAsync(default, default).ReturnsForAnyArgs(new Func<NSubstitute.Core.CallInfo, CustomTagEntry>((x) => { throw new Exception(); }));
            await Assert.ThrowsAsync<Exception>(() => _customTagService.DeleteCustomTagAsync(DicomTag.DeviceSerialNumber.GetPath()));
        }

        [Fact]
        public async Task GivenValidTagPath_WhenDeleteCustomTagIsInvoked_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            string tagPath = tag.GetPath();
            CustomTagEntry entry = tag.BuildCustomTagEntry();
            _customTagStore.GetCustomTagAsync(default, default).ReturnsForAnyArgs(Task.FromResult(entry));
            await _customTagService.DeleteCustomTagAsync(tagPath);
            await _customTagStore.ReceivedWithAnyArgs(1)
                .DeleteCustomTagAsync(default, default);
        }
    }
}
