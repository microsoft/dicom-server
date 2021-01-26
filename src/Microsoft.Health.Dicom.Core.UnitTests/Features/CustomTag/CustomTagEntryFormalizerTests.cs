// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed
{
    public class CustomTagEntryFormalizerTests
    {
        private ICustomTagEntryFormalizer _customTagEntryFormalizer;

        public CustomTagEntryFormalizerTests()
        {
            _customTagEntryFormalizer = new CustomTagEntryFormalizer(new DicomTagParser());
        }

        [MemberData(nameof(GetValidCustomTagEntries))]
        [Theory]
        public void GivenValidCustomTagEntry_WhenFormalizing_ThenShouldReturnSameEntry(CustomTagEntry entry)
        {
            CustomTagEntry formalized = _customTagEntryFormalizer.Formalize(entry);
            Assert.Equal(entry.Path, formalized.Path);
            Assert.Equal(entry.VR, formalized.VR);
            Assert.Equal(entry.Level, formalized.Level);
        }

        [InlineData(null)]
        [InlineData("")]
        [Theory]

        public void GivenStandardTagWithoutVR_WhenFormalizing_ThenVRShouldBeFilled(string vr)
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry entry = new CustomTagEntry(tag.GetPath(), vr, CustomTagLevel.Instance);
            CustomTagEntry formalized = _customTagEntryFormalizer.Formalize(entry);
            Assert.Equal(tag.GetDefaultVR().Code, formalized.VR);
        }

        [Fact]
        public void GivenStandardTagWithVR_WhenFormalizing_ThenVRShouldNotBeUpdated()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            string vr = DicomVR.CS.Code;
            CustomTagEntry entry = new CustomTagEntry(tag.GetPath(), vr, CustomTagLevel.Instance);
            CustomTagEntry formalized = _customTagEntryFormalizer.Formalize(entry);
            Assert.Equal(vr, formalized.VR);
        }

        [Fact]
        public void GivenTagOfLowerCase_WhenFormalizing_ThenTagShouldBeUpperCase()
        {
            DicomTag tag = DicomTag.DeviceLabel;
            CustomTagEntry entry = new CustomTagEntry(tag.GetPath().ToLowerInvariant(), tag.GetDefaultVR().Code, CustomTagLevel.Instance);
            CustomTagEntry formalized = _customTagEntryFormalizer.Formalize(entry);
            Assert.Equal(entry.Path.ToUpperInvariant(), formalized.Path);
        }

        [Fact]
        public void GivenVROfLowerCase_WhenFormalizing_ThenVRShouldBeUpperCase()
        {
            DicomTag tag = DicomTag.DeviceLabel;
            CustomTagEntry entry = new CustomTagEntry(tag.GetPath(), tag.GetDefaultVR().Code.ToLowerInvariant(), CustomTagLevel.Instance);
            CustomTagEntry formalized = _customTagEntryFormalizer.Formalize(entry);
            Assert.Equal(entry.VR.ToUpperInvariant(), formalized.VR);
        }

        [Fact]

        public void GivenStandardTagAsKeyword_WhenFormalizing_ThenVRShouldBeFilled()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry entry = new CustomTagEntry(path: tag.DictionaryEntry.Keyword, tag.GetDefaultVR().Code, CustomTagLevel.Instance);
            string expectedPath = tag.GetPath();
            CustomTagEntry formalized = _customTagEntryFormalizer.Formalize(entry);
            Assert.Equal(formalized.Path, expectedPath);
        }

        public static IEnumerable<object[]> GetValidCustomTagEntries()
        {
            yield return new object[] { DicomTag.DeviceSerialNumber.BuildCustomTagEntry() }; // standard custom tag with VR
            yield return new object[] { new CustomTagEntry("12051003", DicomVRCode.OB, CustomTagLevel.Instance) }; // private tag with VR
        }
    }
}
