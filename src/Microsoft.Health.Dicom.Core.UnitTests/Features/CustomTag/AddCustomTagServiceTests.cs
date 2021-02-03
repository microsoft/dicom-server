// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
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
    public class AddCustomTagServiceTests
    {
        private ICustomTagEntryValidator _customTagEntryValidator;
        private ICustomTagStore _customTagStore;
        private IAddCustomTagService _customTagService;

        public AddCustomTagServiceTests()
        {
            _customTagEntryValidator = Substitute.For<ICustomTagEntryValidator>();
            _customTagStore = Substitute.For<ICustomTagStore>();
            _customTagService = new AddCustomTagService(_customTagStore, _customTagEntryValidator, NullLogger<AddCustomTagService>.Instance);
        }

        [Fact]
        public async Task GivenValidInput_WhenAddCustomTagIsInvoked_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry entry = tag.BuildCustomTagEntry();
            await _customTagService.AddCustomTagAsync(new CustomTagEntry[] { entry }, default);

            _customTagEntryValidator.ReceivedWithAnyArgs().ValidateCustomTags(default);
            await _customTagStore.ReceivedWithAnyArgs().AddCustomTagsAsync(default, default);
        }

        [Fact]
        public async Task GivenInvalidInput_WhenAddCustomTagIsInvoked_ThenStopAfterValidation()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntryValidationException exception = new CustomTagEntryValidationException(string.Empty);
            CustomTagEntry entry = tag.BuildCustomTagEntry();
            _customTagEntryValidator.WhenForAnyArgs(validator => validator.ValidateCustomTags(default))
                .Throw(exception);

            await Assert.ThrowsAsync<CustomTagEntryValidationException>(() => _customTagService.AddCustomTagAsync(new CustomTagEntry[] { entry }, default));
            _customTagEntryValidator.ReceivedWithAnyArgs().ValidateCustomTags(default);
            await _customTagStore.DidNotReceiveWithAnyArgs().AddCustomTagsAsync(default, default);
        }

        [Fact]
        public async Task GivenInputTagPath_WhenDeleteCustomTagIsInvoked_ThenShouldThrowException()
        {
            await Assert.ThrowsAsync<InvalidCustomTagPathException>(() => _customTagService.DeleteCustomTagAsync("0000000A"));
        }

        [Fact]
        public async Task GivenNotExistingTagPath_WhenDeleteCustomTagIsInvoked_ThenShouldThrowException()
        {
            _customTagStore.GetCustomTagsAsync(default, default).ReturnsForAnyArgs(Task.FromResult<IEnumerable<CustomTagEntry>>(new CustomTagEntry[0]));
            await Assert.ThrowsAsync<CustomTagNotFoundException>(() => _customTagService.DeleteCustomTagAsync(DicomTag.DeviceSerialNumber.GetPath()));
        }

        [Fact]
        public async Task GivenValidTagPath_WhenDeleteCustomTagIsInvoked_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            string tagPath = tag.GetPath();
            CustomTagEntry entry = tag.BuildCustomTagEntry();
            _customTagStore.GetCustomTagsAsync(default, default).ReturnsForAnyArgs(Task.FromResult<IEnumerable<CustomTagEntry>>(new CustomTagEntry[] { entry }));
            _customTagStore.DeleteCustomTagStringIndexAsync(tagPath, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(0);
            await _customTagService.DeleteCustomTagAsync(tagPath);

            await _customTagStore.ReceivedWithAnyArgs(1)
                .StartDeleteCustomTagAsync(default, default);
            await _customTagStore.ReceivedWithAnyArgs(1)
                .DeleteCustomTagStringIndexAsync(default, default);
            await _customTagStore.ReceivedWithAnyArgs(1)
                .CompleteDeleteCustomTagAsync(default, default);
        }

        [Fact]
        public async Task GivenLotsOfCustomTagIndex_WhenDeleteCustomTagIsInvoked_ThenShouldCallDeleteFunctionMultipleTimes()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            string tagPath = tag.GetPath();
            CustomTagEntry entry = tag.BuildCustomTagEntry();
            int maxDeleteRecordCount = 10000;
            _customTagStore.GetCustomTagsAsync(default, default).ReturnsForAnyArgs(Task.FromResult<IEnumerable<CustomTagEntry>>(new CustomTagEntry[] { entry }));
            _customTagStore.DeleteCustomTagStringIndexAsync(tagPath, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(
                Task.FromResult((long)maxDeleteRecordCount),
                Task.FromResult((long)maxDeleteRecordCount),
                Task.FromResult((long)maxDeleteRecordCount - 1));

            await _customTagService.DeleteCustomTagAsync(tagPath);
            await _customTagStore.ReceivedWithAnyArgs(3)
                .DeleteCustomTagStringIndexAsync(default, default);
        }
    }
}
