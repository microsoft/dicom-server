// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;
using Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed
{
    public class CustomTagServiceTests
    {
        private IReindexJob _reindexJob;
        private ICustomTagEntryValidator _customTagEntryValidator;
        private ICustomTagStore _customTagStore;
        private ICustomTagService _customTagService;

        public CustomTagServiceTests()
        {
            _reindexJob = Substitute.For<IReindexJob>();
            _customTagEntryValidator = Substitute.For<ICustomTagEntryValidator>();
            _customTagStore = Substitute.For<ICustomTagStore>();
            _customTagService = new CustomTagService(_customTagStore, _reindexJob, _customTagEntryValidator, new DicomTagParser(), NullLogger<CustomTagService>.Instance);
        }

        [Fact]
        public async Task GivenValidInput_WhenAddCustomTagIsInvoked_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.ManufacturerModelName;
            _customTagStore.GetLatestInstanceAsync(default).ReturnsForAnyArgs(1);
            _customTagStore.AddCustomTagAsync(default, default, default, default, default).ReturnsForAnyArgs(1);
            IEnumerable<CustomTagEntry> entries = new CustomTagEntry[]
            {
                tag.BuildCustomTagEntry(),
            };
            AddCustomTagResponse response = await _customTagService.AddCustomTagAsync(entries);

            _customTagEntryValidator.ReceivedWithAnyArgs()
               .ValidateCustomTags(default);

            await _customTagStore.ReceivedWithAnyArgs()
                .GetLatestInstanceAsync(default);

            await _reindexJob.ReceivedWithAnyArgs()
                .ReindexAsync(default, default, default);
        }

        [Fact]
        public async Task GivenInvalidInput_WhenAddCustomTagIsInvoked_ThenShouldFailAtValidation()
        {
            _customTagEntryValidator.WhenForAnyArgs(x => x.ValidateCustomTags(default))
                .Throw(new Exception());
            IEnumerable<CustomTagEntry> entries = new CustomTagEntry[]
            {
                DicomTag.ManufacturerModelName.BuildCustomTagEntry(),
            };

            await Assert.ThrowsAsync<Exception>(() => _customTagService.AddCustomTagAsync(entries));

            await _customTagStore.DidNotReceiveWithAnyArgs()
                .AddCustomTagAsync(default, default, default, default, default);

            await _reindexJob.DidNotReceiveWithAnyArgs()
                .ReindexAsync(default, default, default);
        }

        [Fact]
        public async Task GivenMultipleCustomTags_WhenFailInTheMiddle_ThenShouldFailAndRollback()
        {
            DicomTag tag1 = DicomTag.ManufacturerModelName;
            DicomTag tag2 = DicomTag.PatientBirthDate;
            CustomTagEntry customTagEntry1 = tag1.BuildCustomTagEntry();
            CustomTagEntry customTagEntry2 = tag2.BuildCustomTagEntry();
            IEnumerable<CustomTagEntry> entries = new CustomTagEntry[]
            {
                customTagEntry1,
                customTagEntry2,
            };

            _customTagStore.AddCustomTagAsync(customTagEntry1.Path, customTagEntry1.VR, customTagEntry1.Level, CustomTagStatus.Reindexing, Arg.Any<CancellationToken>())
               .Returns(1);
            _customTagStore.AddCustomTagAsync(customTagEntry2.Path, customTagEntry2.VR, customTagEntry2.Level, CustomTagStatus.Reindexing, Arg.Any<CancellationToken>())
                .Throws(new Exception());

            await Assert.ThrowsAsync<Exception>(() => _customTagService.AddCustomTagAsync(entries));

            await _customTagStore.ReceivedWithAnyArgs()
                .DeleteCustomTagAsync(default, default);
        }

        [Fact]
        public async Task GivenValidInput_WhenThereIsNoInstance_ThenShouldNotReindex()
        {
            DicomTag tag = DicomTag.ManufacturerModelName;
            _customTagStore.GetLatestInstanceAsync(default)
                .ReturnsForAnyArgs((long?)null);
            _customTagStore.AddCustomTagAsync(default, default, default, default, default).ReturnsForAnyArgs(1);
            IEnumerable<CustomTagEntry> entries = new CustomTagEntry[]
            {
                tag.BuildCustomTagEntry(),
            };

            AddCustomTagResponse response = await _customTagService.AddCustomTagAsync(entries);

            await _reindexJob.DidNotReceiveWithAnyArgs()
                .ReindexAsync(default, default, default);
        }

        [Fact]
        public async Task GivenKeywordAsTagPath_WhenAddCustomTagIsInvoked_ThenShouldConvertToAttributeId()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            _customTagStore.AddCustomTagAsync(default, default, default, default, default).ReturnsForAnyArgs(1);
            CustomTagEntry entry = new CustomTagEntry(path: tag.DictionaryEntry.Keyword, tag.GetDefaultVR().Code, CustomTagLevel.Instance);
            IEnumerable<CustomTagEntry> entries = new CustomTagEntry[] { entry };
            AddCustomTagResponse response = await _customTagService.AddCustomTagAsync(entries);
            await _customTagStore.Received(1).AddCustomTagAsync(tag.GetPath(), entry.VR, entry.Level, Arg.Any<CustomTagStatus>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenStandardTagWithoutVR_WhenAddCustomTagIsInvoked_ThenShouldUseDefaultVR()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            _customTagStore.AddCustomTagAsync(default, default, default, default, default).ReturnsForAnyArgs(1);
            CustomTagEntry entry = new CustomTagEntry(path: tag.GetPath(), string.Empty, CustomTagLevel.Instance);
            IEnumerable<CustomTagEntry> entries = new CustomTagEntry[] { entry };
            AddCustomTagResponse response = await _customTagService.AddCustomTagAsync(entries);
            await _customTagStore.Received(1).AddCustomTagAsync(entry.Path, tag.GetDefaultVR().Code, entry.Level, Arg.Any<CustomTagStatus>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenStandardTagWithVR_WhenAddCustomTagIsInvoked_ThenShouldNotUseDefaultVR()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            _customTagStore.AddCustomTagAsync(default, default, default, default, default).ReturnsForAnyArgs(1);
            CustomTagEntry entry = new CustomTagEntry(path: tag.GetPath(), DicomVR.CS.Code, CustomTagLevel.Instance); // Default VR is LO
            IEnumerable<CustomTagEntry> entries = new CustomTagEntry[] { entry };
            AddCustomTagResponse response = await _customTagService.AddCustomTagAsync(entries);
            await _customTagStore.Received(1).AddCustomTagAsync(entry.Path, entry.VR, entry.Level, Arg.Any<CustomTagStatus>(), Arg.Any<CancellationToken>());
        }
    }
}
