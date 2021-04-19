// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed
{
    public class ExtendedQueryTagEntryValidatorTests
    {
        private IExtendedQueryTagEntryValidator _extendedQueryTagEntryValidator;

        public ExtendedQueryTagEntryValidatorTests()
        {
            _extendedQueryTagEntryValidator = new ExtendedQueryTagEntryValidator(new DicomTagParser());
        }

        [Fact]
        public void GivenNoExtendedQueryTagEntry_WhenValidating_ThenShouldThrowException()
        {
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => { _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[0]); });
        }

        [Fact]
        public void GivenMissingLevel_WhenValidating_ThenShouldThrowException()
        {
            AddExtendedQueryTagEntry entry = new AddExtendedQueryTagEntry { Path = "00101060", VR = "PN" };
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry }));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("BABC")]
        [InlineData("0018B001")]
        public void GivenInvalidTag_WhenValidating_ThenShouldThrowException(string path)
        {
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(path, DicomVRCode.AE);
            var ex = Assert.Throws<ExtendedQueryTagEntryValidationException>(() => { _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry }); });
            Assert.Equal(string.Format("The extended query tag '{0}' is invalid as it cannot be parsed into a valid Dicom Tag.", path), ex.Message);
        }

        [Theory]
        [InlineData("0040A30a")] // lower case is also supported
        [InlineData("0040A30A")]
        public void GivenValidTag_WhenValidating_ThenShouldSucceed(string path)
        {
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(path, DicomVRCode.DS);
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GivenStandardTagWithoutVR_WhenValidating_ThenShouldSucceed(string vr)
        {
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(DicomTag.DeviceSerialNumber.GetPath(), vr);
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry });
        }

        [Fact]
        public void GivenStandardTagWithPrivateCreator_WhenValidating_ThenShouldThrowExceptoin()
        {
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(DicomTag.DeviceSerialNumber.GetPath(), null, privateCreator: "PrivateCreator");
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() =>
            {
                _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry });
            });
        }

        [Fact] 
        public void GivenInvalidVRForTag_WhenValidating_ThenShouldThrowException()
        {
            string tagPath = DicomTag.DeviceSerialNumber.GetPath();
            string vr = "CS"; // expected vr should be LO. CS is not acceptable
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(tagPath, vr);
            var ex = Assert.Throws<ExtendedQueryTagEntryValidationException>(() => { _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry }); });
            Assert.Equal(string.Format("The VR code '{0}' is incorrectly specified for '{1}'. The expected VR code for it is '{2}'. Retry this request either with the correct VR code or without specifying it.", vr, tagPath, "LO"), ex.Message);
        }

        [Fact]
        public void GivenInvalidVR_WhenValidating_ThenShouldThrowException()
        {
            string tagPath = DicomTag.DeviceSerialNumber.GetPath();
            string vr = "LOX";
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(tagPath, vr);
            var ex = Assert.Throws<ExtendedQueryTagEntryValidationException>(() => { _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry }); });
            Assert.Equal(string.Format("The VR code '{0}' for tag '{1}' is invalid.", vr, tagPath), ex.Message);
        }

        [Theory]
        [InlineData("0018A001", DicomVRCode.SQ, DicomVRCode.SQ)]
        [InlineData("0018A001", "", DicomVRCode.SQ)] // when VR is missing for standard tag
        public void GivenUnsupportedVR_WhenValidating_ThenShouldThrowException(string path, string vr, string expectedVR)
        {
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(path, vr);
            var ex = Assert.Throws<ExtendedQueryTagEntryValidationException>(() => { _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry }); });
            Assert.Equal(string.Format("The VR code '{0}' specified for tag '{1}' is not supported.", expectedVR, path), ex.Message);
        }



        [Theory]
        [InlineData("Lo")] // verify lower case
        [InlineData("LO")]
        public void GivenValidVR_WhenValidating_ThenShouldSucceed(string vr)
        {
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(DicomTag.DeviceSerialNumber.GetPath(), vr);
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry });
        }

        [Fact]
        public void GivenPrivateTagWithoutVR_WhenValidating_ThenShouldThrowException()
        {
            string path = "12051003";
            string vr = string.Empty;
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(path, vr, "PrivateCreator1");
            var ex = Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry }));
            Assert.Equal(string.Format("The vr for tag '12051003' is missing.", path), ex.Message);
        }

        [Fact]
        public void GivenPrivateTagWithoutPrivateCreator_WhenValidating_ThenShouldThrowException()
        {
            string path = "12051003";
            string vr = DicomVRCode.OB;
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(path, vr);
            var ex = Assert.Throws<ExtendedQueryTagEntryValidationException>(() => { _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry }); });
            Assert.Equal(string.Format("The private creator for private tag '{0}' is missing.", path), ex.Message);
        }

        [Fact]
        public void GivenPrivateTagWithTooLongPrivateCreator_WhenValidating_ThenShouldThrowException()
        {
            // max length of PrivateCreator is 64
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry("12051003", DicomVRCode.CS, new string('c', 65));
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry }));
        }

        [Fact]
        public void GivenPrivateTagWithVR_WhenValidating_ThenShouldSucceed()
        {
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry("12051003", DicomVRCode.AE, "PrivateCreator1");
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry });
        }

        [Fact]
        public void GivenSupportedTag_WhenValidating_ThenShouldThrowException()
        {
            AddExtendedQueryTagEntry entry = DicomTag.PatientName.BuildAddExtendedQueryTagEntry();
            var ex = Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry }));
            Assert.Equal(string.Format("The query tag '{0}' is already supported.", entry.Path), ex.Message);
        }

        [Fact]
        public void GivenValidAndInvalidTags_WhenValidating_ThenShouldThrowException()
        {
            AddExtendedQueryTagEntry invalidEntry = DicomTag.PatientName.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry validEntry = DicomTag.DeviceSerialNumber.BuildAddExtendedQueryTagEntry();
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { validEntry, invalidEntry }));
        }

        [Fact]
        public void GivenDuplicatedTag_WhenValidating_ThenShouldThrowException()
        {
            AddExtendedQueryTagEntry entry = DicomTag.PatientName.BuildAddExtendedQueryTagEntry();
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry, entry }));
        }

        [Fact]
        public void GivenPrivateIdentificationCodeWithoutVR_WhenValidating_ThenShouldSucceed()
        {
            DicomTag dicomTag = new DicomTag(0x2201, 0x0010);
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(dicomTag.GetPath(), null);
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry });
        }

        [Fact]
        public void GivenPrivateIdentificationCodeWithWrongVR_WhenValidating_ThenShouldSucceed()
        {
            DicomTag dicomTag = new DicomTag(0x2201, 0x0010);
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(dicomTag.GetPath(), DicomVR.AE.Code);
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new AddExtendedQueryTagEntry[] { entry }));
        }


        private static AddExtendedQueryTagEntry CreateExtendedQueryTagEntry(string path, string vr, string privateCreator = null, QueryTagLevel level = QueryTagLevel.Instance)
        {
            return new AddExtendedQueryTagEntry { Path = path, VR = vr, PrivateCreator = privateCreator, Level = level.ToString() };
        }
    }
}
