// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed
{
    public class CustomTagEntryValidatorTests
    {
        private ICustomTagEntryValidator _customTagEntryValidator;

        public CustomTagEntryValidatorTests()
        {
            _customTagEntryValidator = new CustomTagEntryValidator();
        }

        [Fact]
        public void GivenNoCustomTagEntry_WhenValidating_ThenShouldThrowException()
        {
            Assert.Throws<CustomTagEntryValidationException>(() => { _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[0]); });
        }

        [InlineData(null)]
        [InlineData("")]
        [InlineData("BABC")]
        [InlineData("0018B001")]
        [Theory]
        public void GivenInvalidTag_WhenValidating_ThenShouldThrowException(string path)
        {
            CustomTagEntry entry = new CustomTagEntry(0, path, DicomVRCode.AE, CustomTagLevel.Instance, CustomTagStatus.Reindexing);
            Assert.Throws<CustomTagEntryValidationException>(() => { _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry }); });
        }

        [InlineData("0040A30a")] // lower case is also supported
        [InlineData("0040A30A")]
        [Theory]
        public void GivenValidTag_WhenValidating_ThenShouldSucceed(string path)
        {
            CustomTagEntry entry = new CustomTagEntry(0, path, DicomVRCode.DS, CustomTagLevel.Instance, CustomTagStatus.Reindexing);
            _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry });
        }

        [InlineData(null)]
        [InlineData("")]
        [Theory]
        public void GivenStandardTagWithoutVR_WhenValidating_ThenShouldSucceed(string vr)
        {
            CustomTagEntry entry = new CustomTagEntry(0, DicomTag.DeviceSerialNumber.GetPath(), vr, CustomTagLevel.Instance, CustomTagStatus.Reindexing);
            _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry });
        }

        [InlineData("LOX")]
        [InlineData("CS")] // expected vr should be LO. CS is not acceptable
        [Theory]
        public void GivenInvalidVR_WhenValidating_ThenShouldThrowException(string vr)
        {
            CustomTagEntry entry = new CustomTagEntry(0, DicomTag.DeviceSerialNumber.GetPath(), vr, CustomTagLevel.Instance, CustomTagStatus.Reindexing);
            Assert.Throws<CustomTagEntryValidationException>(() => { _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry }); });
        }

        [InlineData("0018A001", DicomVRCode.SQ)]
        [InlineData("0018A001", "")] // when VR is missing for standard tag
        [InlineData("12051003", DicomVRCode.OB)] // private tag
        [Theory]
        public void GivenUnsupportedVR_WhenValidating_ThenShouldThrowException(string path, string vr)
        {
            CustomTagEntry entry = new CustomTagEntry(0, path, vr, CustomTagLevel.Instance, CustomTagStatus.Reindexing);
            Assert.Throws<CustomTagEntryValidationException>(() => { _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry }); });
        }

        [InlineData("Lo")] // verify lower case
        [InlineData("LO")]
        [Theory]
        public void GivenValidVR_WhenValidating_ThenShouldSucceed(string vr)
        {
            CustomTagEntry entry = new CustomTagEntry(0, DicomTag.DeviceSerialNumber.GetPath(), vr, CustomTagLevel.Instance, CustomTagStatus.Reindexing);
            _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry });
        }

        [Fact]
        public void GivenPrivateTagWithoutVR_WhenValidating_ThenShouldThrowException()
        {
            CustomTagEntry entry = new CustomTagEntry(0, "12051003", string.Empty, CustomTagLevel.Instance, CustomTagStatus.Reindexing);
            Assert.Throws<CustomTagEntryValidationException>(() => _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry }));
        }

        [Fact]
        public void GivenPrivateTagWithVR_WhenValidating_ThenShouldSucceed()
        {
            CustomTagEntry entry = new CustomTagEntry(0, "12051003", DicomVRCode.AE, CustomTagLevel.Instance, CustomTagStatus.Reindexing);
            _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry });
        }

        [Fact]
        public void GivenSupportedTag_WhenValidating_ThenShouldThrowException()
        {
            CustomTagEntry entry = DicomTag.PatientName.BuildCustomTagEntry();
            Assert.Throws<CustomTagEntryValidationException>(() => _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry }));
        }

        [Fact]
        public void GivenValidAndInvalidTags_WhenValidating_ThenShouldThrowException()
        {
            CustomTagEntry invalidEntry = DicomTag.PatientName.BuildCustomTagEntry();
            CustomTagEntry validEntry = DicomTag.DeviceSerialNumber.BuildCustomTagEntry();
            Assert.Throws<CustomTagEntryValidationException>(() => _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { validEntry, invalidEntry }));
        }

        [Fact]
        public void GivenDuplicatedTag_WhenValidating_ThenShouldThrowException()
        {
            CustomTagEntry entry = DicomTag.PatientName.BuildCustomTagEntry();
            Assert.Throws<CustomTagEntryValidationException>(() => _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry, entry }));
        }
    }
}
