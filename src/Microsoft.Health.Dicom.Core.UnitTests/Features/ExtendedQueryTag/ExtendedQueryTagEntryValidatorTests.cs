// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using Dicom;
using Microsoft.Extensions.Options;
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
            _extendedQueryTagEntryValidator = new ExtendedQueryTagEntryValidator(new DicomTagParser(), Options.Create(new Configs.ExtendedQueryTagConfiguration()));
        }

        [Fact]
        public void GivenNoExtendedQueryTagEntry_WhenValidating_ThenShouldThrowException()
        {
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => { _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[0]); });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("BABC")]
        [InlineData("0018B001")]
        public void GivenInvalidTag_WhenValidating_ThenShouldThrowException(string path)
        {
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(path, DicomVRCode.AE);
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => { _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry }); });
        }

        [Theory]
        [InlineData("0040A30a")] // lower case is also supported
        [InlineData("0040A30A")]
        public void GivenValidTag_WhenValidating_ThenShouldSucceed(string path)
        {
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(path, DicomVRCode.DS);
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GivenStandardTagWithoutVR_WhenValidating_ThenShouldSucceed(string vr)
        {
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(DicomTag.DeviceSerialNumber.GetPath(), vr);
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry });
        }

        [Fact]
        public void GivenStandardTagWithPrivateCreator_WhenValidating_ThenShouldThrowExceptoin()
        {
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(DicomTag.DeviceSerialNumber.GetPath(), null, privateCreator: "PrivateCreator");
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() =>
            {
                _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry });
            });
        }

        [Theory]
        [InlineData("LOX")]
        [InlineData("CS")] // expected vr should be LO. CS is not acceptable
        public void GivenInvalidVR_WhenValidating_ThenShouldThrowException(string vr)
        {
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(DicomTag.DeviceSerialNumber.GetPath(), vr);
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => { _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry }); });
        }

        [Theory]
        [InlineData("0018A001", DicomVRCode.SQ)]
        [InlineData("0018A001", "")] // when VR is missing for standard tag
        [InlineData("12051003", DicomVRCode.OB)] // private tag
        public void GivenUnsupportedVR_WhenValidating_ThenShouldThrowException(string path, string vr)
        {
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(path, vr);
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => { _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry }); });
        }

        [Theory]
        [InlineData("Lo")] // verify lower case
        [InlineData("LO")]
        public void GivenValidVR_WhenValidating_ThenShouldSucceed(string vr)
        {
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(DicomTag.DeviceSerialNumber.GetPath(), vr);
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry });
        }

        [Fact]
        public void GivenPrivateTagWithoutVR_WhenValidating_ThenShouldThrowException()
        {
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry("12051003", string.Empty, "PrivateCreator1");
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry }));
        }

        [Fact]
        public void GivenPrivateTagWithoutPrivateCreator_WhenValidating_ThenShouldThrowException()
        {
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry("12051003", DicomVRCode.CS, string.Empty);
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry }));
        }

        [Fact]
        public void GivenPrivateTagWithTooLongPrivateCreator_WhenValidating_ThenShouldThrowException()
        {
            // max length of PrivateCreator is 64
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry("12051003", DicomVRCode.CS, new string('c', 65));
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry }));
        }

        [Fact]
        public void GivenPrivateTagWithVR_WhenValidating_ThenShouldSucceed()
        {
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry("12051003", DicomVRCode.AE, "PrivateCreator1");
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry });
        }

        [Fact]
        public void GivenSupportedTag_WhenValidating_ThenShouldThrowException()
        {
            ExtendedQueryTagEntry entry = DicomTag.PatientName.BuildExtendedQueryTagEntry();
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry }));
        }

        [Fact]
        public void GivenValidAndInvalidTags_WhenValidating_ThenShouldThrowException()
        {
            ExtendedQueryTagEntry invalidEntry = DicomTag.PatientName.BuildExtendedQueryTagEntry();
            ExtendedQueryTagEntry validEntry = DicomTag.DeviceSerialNumber.BuildExtendedQueryTagEntry();
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { validEntry, invalidEntry }));
        }

        [Fact]
        public void GivenDuplicatedTag_WhenValidating_ThenShouldThrowException()
        {
            ExtendedQueryTagEntry entry = DicomTag.PatientName.BuildExtendedQueryTagEntry();
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry, entry }));
        }

        [Fact]
        public void GivenPrivateIdentificationCodeWithoutVR_WhenValidating_ThenShouldSucceed()
        {
            DicomTag dicomTag = new DicomTag(0x2201, 0x0010);
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(dicomTag.GetPath(), null);
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry });
        }

        [Fact]
        public void GivenPrivateIdentificationCodeWithWrongVR_WhenValidating_ThenShouldSucceed()
        {
            DicomTag dicomTag = new DicomTag(0x2201, 0x0010);
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(dicomTag.GetPath(), DicomVR.AE.Code);
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(new ExtendedQueryTagEntry[] { entry }));
        }

        [Fact]
        public void GivenTooManyTags_WhenValidating_ThenShouldThrowException()
        {
            DicomTag dicomTag = new DicomTag(0x2201, 0x0010);
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(dicomTag.GetPath(), DicomVR.AE.Code);
            Assert.Throws<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(Enumerable.Repeat(entry, 129)));
        }


        private static ExtendedQueryTagEntry CreateExtendedQueryTagEntry(string path, string vr, string privateCreator = null, QueryTagLevel level = QueryTagLevel.Instance, ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Ready)
        {
            return new ExtendedQueryTagEntry { Path = path, VR = vr, PrivateCreator = privateCreator, Level = level, Status = status };
        }
    }
}
