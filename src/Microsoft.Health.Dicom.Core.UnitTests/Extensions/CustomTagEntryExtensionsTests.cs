// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions
{
    public class CustomTagEntryExtensionsTests
    {
        [Theory]
        [MemberData(nameof(GetValidCustomTagEntries))]
        public void GivenValidCustomTagEntry_WhenNormalizing_ThenShouldReturnSameEntry(CustomTagEntry entry)
        {
            CustomTagEntry normalized = entry.Normalize(CustomTagStatus.Adding);
            Assert.Equal(entry.Path, normalized.Path);
            Assert.Equal(entry.VR, normalized.VR);
            Assert.Equal(entry.Level, normalized.Level);
            Assert.Equal(CustomTagStatus.Adding, normalized.Status);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void GivenPrivateTagWithNonEmptyPrivateCreator_WhenNormalizing_ThenPrivateCreatorShouldBeNull(string privateCreator)
        {
            DicomTag tag1 = new DicomTag(0x0405, 0x1001, "PrivateCreator1");
            CustomTagEntry normalized = tag1.BuildCustomTagEntry(vr: DicomVRCode.CS, privateCreator: privateCreator).Normalize(CustomTagStatus.Ready);
            Assert.Null(normalized.PrivateCreator);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]

        public void GivenStandardTagWithoutVR_WhenNormalizing_ThenVRShouldBeFilled(string vr)
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry entry = CreateCustomTagEntry(tag.GetPath(), vr, null, CustomTagLevel.Instance, CustomTagStatus.Ready);
            CustomTagEntry normalized = entry.Normalize(CustomTagStatus.Adding);
            Assert.Equal(tag.GetDefaultVR().Code, normalized.VR);
        }

        [Fact]
        public void GivenStandardTagWithVR_WhenNormalizing_ThenVRShouldNotBeUpdated()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            string vr = DicomVR.CS.Code;
            CustomTagEntry entry = CreateCustomTagEntry(tag.GetPath(), vr, null, CustomTagLevel.Instance, CustomTagStatus.Ready);
            CustomTagEntry normalized = entry.Normalize(CustomTagStatus.Adding);
            Assert.Equal(vr, normalized.VR);
        }

        [Fact]
        public void GivenTagOfLowerCase_WhenNormalizing_ThenTagShouldBeUpperCase()
        {
            DicomTag tag = DicomTag.DeviceLabel;
            CustomTagEntry entry = CreateCustomTagEntry(tag.GetPath().ToLowerInvariant(), tag.GetDefaultVR().Code, null, CustomTagLevel.Instance, CustomTagStatus.Ready);
            CustomTagEntry normalized = entry.Normalize(CustomTagStatus.Adding);
            Assert.Equal(entry.Path.ToUpperInvariant(), normalized.Path);
        }

        [Fact]
        public void GivenVROfLowerCase_WhenNormalizing_ThenVRShouldBeUpperCase()
        {
            DicomTag tag = DicomTag.DeviceLabel;
            CustomTagEntry entry = CreateCustomTagEntry(tag.GetPath(), tag.GetDefaultVR().Code.ToLowerInvariant(), null, CustomTagLevel.Instance, CustomTagStatus.Ready);
            CustomTagEntry normalized = entry.Normalize(CustomTagStatus.Adding);
            Assert.Equal(entry.VR.ToUpperInvariant(), normalized.VR);
        }

        [Fact]

        public void GivenStandardTagAsKeyword_WhenNormalizing_ThenVRShouldBeFilled()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry entry = CreateCustomTagEntry(path: tag.DictionaryEntry.Keyword, tag.GetDefaultVR().Code, null, CustomTagLevel.Instance, CustomTagStatus.Ready);
            string expectedPath = tag.GetPath();
            CustomTagEntry normalized = entry.Normalize(CustomTagStatus.Adding);
            Assert.Equal(normalized.Path, expectedPath);
        }

        public static IEnumerable<object[]> GetValidCustomTagEntries()
        {
            yield return new object[] { DicomTag.DeviceSerialNumber.BuildCustomTagEntry() }; // standard custom tag with VR
            yield return new object[] { CreateCustomTagEntry("12051003", DicomVRCode.OB, "PrivateCreator1", CustomTagLevel.Instance, CustomTagStatus.Ready) }; // private tag with VR
        }

        private static CustomTagEntry CreateCustomTagEntry(string path, string vr, string privateCreator, CustomTagLevel level = CustomTagLevel.Instance, CustomTagStatus status = CustomTagStatus.Ready)
        {
            return new CustomTagEntry { Path = path, VR = vr, PrivateCreator = privateCreator, Level = level, Status = status };
        }
    }
}
