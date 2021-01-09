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
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;
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
            _customTagService = new CustomTagService(_customTagStore, _reindexJob, _customTagEntryValidator, NullLogger<CustomTagService>.Instance);
        }

        [Fact]
        public async Task GivenValidInput_WhenAddCustomTagIsInvoked_ThenShouldSucceed()
        {
            _customTagStore.GetLatestInstanceAsync(default).ReturnsForAnyArgs(1);
            IEnumerable<CustomTagEntry> entries = new CustomTagEntry[]
            {
                FromDicomTag(DicomTag.ManufacturerModelName),
            };
            AddCustomTagResponse response = await _customTagService.AddCustomTagAsync(entries);

            await _customTagEntryValidator.ReceivedWithAnyArgs()
                .ValidateCustomTagsAsync(default, default);

            await _customTagStore.ReceivedWithAnyArgs()
                .GetLatestInstanceAsync(default);

            await _reindexJob.ReceivedWithAnyArgs()
                .ReindexAsync(default, default, default);

            // Verify status
            foreach (CustomTagEntry item in response.CustomTags)
            {
                Assert.Equal(CustomTagStatus.Added, item.Status);
            }
        }

        [Fact]
        public async Task GivenInvalidInput_WhenAddCustomTagIsInvoked_ThenShouldFailAtValidation()
        {
            _customTagEntryValidator.ValidateCustomTagsAsync(default, default).ThrowsForAnyArgs(new Exception());
            IEnumerable<CustomTagEntry> entries = new CustomTagEntry[]
            {
                FromDicomTag(DicomTag.ManufacturerModelName),
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
            CustomTagEntry customTagEntry1 = FromDicomTag(DicomTag.ManufacturerModelName);
            CustomTagEntry customTagEntry2 = FromDicomTag(DicomTag.PatientBirthDate);
            IEnumerable<CustomTagEntry> entries = new CustomTagEntry[]
            {
                customTagEntry1,
                customTagEntry2,
            };

            _customTagStore.AddCustomTagAsync(customTagEntry2.Path, customTagEntry2.VR, customTagEntry2.Level, customTagEntry2.Status, Arg.Any<CancellationToken>())
                .Throws(new Exception());

            await Assert.ThrowsAsync<Exception>(() => _customTagService.AddCustomTagAsync(entries));

            await _customTagStore.ReceivedWithAnyArgs()
                .DeleteCustomTagAsync(default, default);
        }

        [Fact]
        public async Task GivenValidInput_WhenThereIsNoInstance_ThenShouldNotReinde()
        {
            _customTagStore.GetLatestInstanceAsync(default)
                .ReturnsForAnyArgs((long?)null);

            IEnumerable<CustomTagEntry> entries = new CustomTagEntry[]
            {
                FromDicomTag(DicomTag.ManufacturerModelName),
            };

            AddCustomTagResponse response = await _customTagService.AddCustomTagAsync(entries);

            await _reindexJob.DidNotReceiveWithAnyArgs()
                .ReindexAsync(default, default, default);
        }

        private static CustomTagEntry FromDicomTag(DicomTag tag, CustomTagLevel level = CustomTagLevel.Series, CustomTagStatus status = CustomTagStatus.Reindexing)
        {
            return new CustomTagEntry(key: 0, path: tag.GetPath(), vr: tag.DictionaryEntry.ValueRepresentations[0].Code, level: level, status: status);
        }
    }
}
