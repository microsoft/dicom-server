// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Dicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
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
            FeatureConfiguration featureConfiguration = new FeatureConfiguration() { EnableExtendedQueryTags = true };
            _extendedQueryTagService = new AddExtendedQueryTagService(_extendedQueryTagStore, _extendedQueryTagEntryValidator, Options.Create(featureConfiguration));
        }

        [Fact]
        public async Task GivenFeatureDisabled_WhenAddExtendedQueryTagIsInvoked_ThenShouldThrowException()
        {
            FeatureConfiguration featureConfiguration = new FeatureConfiguration() { EnableExtendedQueryTags = false };
            IAddExtendedQueryTagService _extendedQueryTagService = new AddExtendedQueryTagService(_extendedQueryTagStore, _extendedQueryTagEntryValidator, Options.Create(featureConfiguration));

            DicomTag tag = DicomTag.DeviceSerialNumber;
            ExtendedQueryTagEntry entry = tag.BuildExtendedQueryTagEntry();
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => _extendedQueryTagService.AddExtendedQueryTagAsync(new ExtendedQueryTagEntry[] { entry }, default));
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
