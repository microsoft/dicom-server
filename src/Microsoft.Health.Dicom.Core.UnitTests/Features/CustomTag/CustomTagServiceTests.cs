// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Dicom;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed
{
    public class CustomTagServiceTests
    {
        private ICustomTagEntryValidator _customTagEntryValidator;
        private ICustomTagStore _customTagStore;
        private ICustomTagService _customTagService;
        private ICustomTagEntryFormalizer _customTagEntryFormalizer;

        public CustomTagServiceTests()
        {
            _customTagEntryValidator = Substitute.For<ICustomTagEntryValidator>();
            _customTagStore = Substitute.For<ICustomTagStore>();
            _customTagEntryFormalizer = Substitute.For<ICustomTagEntryFormalizer>();
            _customTagService = new CustomTagService(_customTagStore, _customTagEntryValidator, _customTagEntryFormalizer, NullLogger<CustomTagService>.Instance);
        }

        [Fact]
        public async Task GivenValidInput_WhenAddCustomTagIsInvoked_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry entry = tag.BuildCustomTagEntry();
            _customTagEntryFormalizer.Formalize(default).ReturnsForAnyArgs(entry);
            await _customTagService.AddCustomTagAsync(new CustomTagEntry[] { entry }, default);

            // Note that _customTagEntryFormalizer won't receive call in this test due to how Linq is executed
            _customTagEntryValidator.ReceivedWithAnyArgs().ValidateCustomTags(default);
            await _customTagStore.ReceivedWithAnyArgs().AddCustomTagsAsync(default, default);
        }

        [Fact]
        public async Task GivenInvalidInput_WhenAddCustomTagIsInvoked_ThenStopAfterValidation()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntryValidationException exception = new CustomTagEntryValidationException(string.Empty);
            CustomTagEntry entry = tag.BuildCustomTagEntry();
            _customTagEntryFormalizer.Formalize(entry).Returns(entry);
            _customTagEntryValidator.WhenForAnyArgs(validator => validator.ValidateCustomTags(default))
                .Throw(exception);

            await Assert.ThrowsAsync<CustomTagEntryValidationException>(() => _customTagService.AddCustomTagAsync(new CustomTagEntry[] { entry }, default));
            _customTagEntryValidator.ReceivedWithAnyArgs().ValidateCustomTags(default);
            _customTagEntryFormalizer.DidNotReceiveWithAnyArgs().Formalize(default);
            await _customTagStore.DidNotReceiveWithAnyArgs().AddCustomTagsAsync(default, default);
        }
    }
}
