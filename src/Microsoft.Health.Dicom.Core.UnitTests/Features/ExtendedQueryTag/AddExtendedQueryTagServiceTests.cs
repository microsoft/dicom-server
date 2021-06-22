// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Dicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
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
            var storeFactory = Substitute.For<IStoreFactory<IExtendedQueryTagStore>>();
            storeFactory.GetInstanceAsync(default).Returns(_extendedQueryTagStore);
            var config = new Configs.ExtendedQueryTagConfiguration();
            _extendedQueryTagService = new AddExtendedQueryTagService(storeFactory, _extendedQueryTagEntryValidator, Options.Create(config));
        }

        [Fact]
        public async Task GivenValidInput_WhenAddExtendedQueryTagIsInvoked_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry entry = tag.BuildAddExtendedQueryTagEntry();
            await _extendedQueryTagService.AddExtendedQueryTagAsync(new AddExtendedQueryTagEntry[] { entry }, default);

            _extendedQueryTagEntryValidator.ReceivedWithAnyArgs().ValidateExtendedQueryTags(default);
            await _extendedQueryTagStore.ReceivedWithAnyArgs().AddExtendedQueryTagsAsync(default, default);
        }

        [Fact]
        public async Task GivenInvalidInput_WhenAddExtendedQueryTagIsInvoked_ThenStopAfterValidation()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            var exception = new ExtendedQueryTagEntryValidationException(string.Empty);
            AddExtendedQueryTagEntry entry = tag.BuildAddExtendedQueryTagEntry();
            _extendedQueryTagEntryValidator.WhenForAnyArgs(validator => validator.ValidateExtendedQueryTags(default))
                .Throw(exception);

            await Assert.ThrowsAsync<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagService.AddExtendedQueryTagAsync(new AddExtendedQueryTagEntry[] { entry }, default));
            _extendedQueryTagEntryValidator.ReceivedWithAnyArgs().ValidateExtendedQueryTags(default);
            await _extendedQueryTagStore.DidNotReceiveWithAnyArgs().AddExtendedQueryTagsAsync(default, default);
        }
    }
}
