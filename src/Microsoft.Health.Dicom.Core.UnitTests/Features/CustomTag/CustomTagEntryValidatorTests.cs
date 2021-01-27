// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
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
            _customTagEntryValidator = new CustomTagEntryValidator(new DicomTagParser());
        }

        [Fact]
        public void GivenNoCustomTagEntry_WhenValidating_ThenShouldThrowException()
        {
            Assert.Throws<CustomTagEntryValidationException>(() => { _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[0]); });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("BABC")]
        [InlineData("0018B001")]
        public void GivenInvalidTag_WhenValidating_ThenShouldThrowException(string path)
        {
            CustomTagEntry entry = CreateCustomTagEntry(path, DicomVRCode.AE);
            Assert.Throws<CustomTagEntryValidationException>(() => { _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry }); });
        }

        [Theory]
        [InlineData("0040A30a")] // lower case is also supported
        [InlineData("0040A30A")]
        public void GivenValidTag_WhenValidating_ThenShouldSucceed(string path)
        {
            CustomTagEntry entry = CreateCustomTagEntry(path, DicomVRCode.DS);
            _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GivenStandardTagWithoutVR_WhenValidating_ThenShouldSucceed(string vr)
        {
            CustomTagEntry entry = CreateCustomTagEntry(DicomTag.DeviceSerialNumber.GetPath(), vr);
            _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry });
        }

        [Theory]
        [InlineData("LOX")]
        [InlineData("CS")] // expected vr should be LO. CS is not acceptable
        public void GivenInvalidVR_WhenValidating_ThenShouldThrowException(string vr)
        {
            CustomTagEntry entry = CreateCustomTagEntry(DicomTag.DeviceSerialNumber.GetPath(), vr);
            Assert.Throws<CustomTagEntryValidationException>(() => { _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry }); });
        }

        [Theory]
        [InlineData("0018A001", DicomVRCode.SQ)]
        [InlineData("0018A001", "")] // when VR is missing for standard tag
        [InlineData("12051003", DicomVRCode.OB)] // private tag
        public void GivenUnsupportedVR_WhenValidating_ThenShouldThrowException(string path, string vr)
        {
            CustomTagEntry entry = CreateCustomTagEntry(path, vr);
            Assert.Throws<CustomTagEntryValidationException>(() => { _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry }); });
        }

        [Theory]
        [InlineData("Lo")] // verify lower case
        [InlineData("LO")]
        public void GivenValidVR_WhenValidating_ThenShouldSucceed(string vr)
        {
            CustomTagEntry entry = CreateCustomTagEntry(DicomTag.DeviceSerialNumber.GetPath(), vr);
            _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry });
        }

        [Fact]
        public void GivenPrivateTagWithoutVR_WhenValidating_ThenShouldThrowException()
        {
            CustomTagEntry entry = CreateCustomTagEntry("12051003", string.Empty);
            Assert.Throws<CustomTagEntryValidationException>(() => _customTagEntryValidator.ValidateCustomTags(new CustomTagEntry[] { entry }));
        }

        [Fact]
        public void GivenPrivateTagWithVR_WhenValidating_ThenShouldSucceed()
        {
            CustomTagEntry entry = CreateCustomTagEntry("12051003", DicomVRCode.AE);
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

        private static CustomTagEntry CreateCustomTagEntry(string path, string vr, CustomTagLevel level = CustomTagLevel.Instance, CustomTagStatus status = CustomTagStatus.Added)
        {
            return new CustomTagEntry(path, vr, level, status);
        }
    }
}
