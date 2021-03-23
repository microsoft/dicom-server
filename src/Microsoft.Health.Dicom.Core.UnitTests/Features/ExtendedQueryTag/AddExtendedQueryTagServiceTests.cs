// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed
{
    public class AddExtendedQueryTagServiceTests
    {
        private readonly IExtendedQueryTagEntryValidator _extendedQueryTagEntryValidator;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IAddExtendedQueryTagService _extendedQueryTagService;

        public AddExtendedQueryTagServiceTests()
        {
            _extendedQueryTagEntryValidator = Substitute.For<IExtendedQueryTagEntryValidator>();
            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _extendedQueryTagService = new AddExtendedQueryTagService(_extendedQueryTagStore, _extendedQueryTagEntryValidator);
        }

        [Fact]
        public async Task GivenValidInput_WhenAddExtendedQueryTagIsInvoked_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            ExtendedQueryTagEntry entry = tag.BuildExtendedQueryTagEntry();
            await _extendedQueryTagService.AddExtendedQueryTagAsync(new ExtendedQueryTagEntry[] { entry }, default);

            _extendedQueryTagEntryValidator.ReceivedWithAnyArgs().ValidateExtendedQueryTags(default);
            await _extendedQueryTagStore.ReceivedWithAnyArgs().AddExtendedQueryTagsAsync(default, default);
        }

        [Fact]
        public async Task GivenInvalidInput_WhenAddExtendedQueryTagIsInvoked_ThenStopAfterValidation()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            var exception = new ExtendedQueryTagEntryValidationException(string.Empty);
            ExtendedQueryTagEntry entry = tag.BuildExtendedQueryTagEntry();
            _extendedQueryTagEntryValidator.WhenForAnyArgs(validator => validator.ValidateExtendedQueryTags(default))
                .Throw(exception);

            await Assert.ThrowsAsync<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagService.AddExtendedQueryTagAsync(new ExtendedQueryTagEntry[] { entry }, default));
            _extendedQueryTagEntryValidator.ReceivedWithAnyArgs().ValidateExtendedQueryTags(default);
            await _extendedQueryTagStore.DidNotReceiveWithAnyArgs().AddExtendedQueryTagsAsync(default, default);
        }
    }
}
