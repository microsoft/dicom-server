// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions
{
    public class ExtendedQueryTagEntryExtensionsTests
    {
        [Theory]
        [MemberData(nameof(GetValidExtendedQueryTagEntries))]
        public void GivenValidExtendedQueryTagEntry_WhenNormalizing_ThenShouldReturnSameEntry(ExtendedQueryTagEntry entry)
        {
            ExtendedQueryTagEntry normalized = entry.Normalize(ExtendedQueryTagStatus.Adding);
            Assert.Equal(entry.Path, normalized.Path);
            Assert.Equal(entry.VR, normalized.VR);
            Assert.Equal(entry.Level, normalized.Level);
            Assert.Equal(ExtendedQueryTagStatus.Adding, normalized.Status);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void GivenPrivateTagWithNonEmptyPrivateCreator_WhenNormalizing_ThenPrivateCreatorShouldBeNull(string privateCreator)
        {
            DicomTag tag1 = new DicomTag(0x0405, 0x1001);
            ExtendedQueryTagEntry normalized = new ExtendedQueryTagEntry() { Level = QueryTagLevel.Instance, Path = tag1.GetPath(), PrivateCreator = privateCreator, VR = DicomVRCode.CS }.Normalize(ExtendedQueryTagStatus.Ready);
            Assert.Null(normalized.PrivateCreator);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]

        public void GivenStandardTagWithoutVR_WhenNormalizing_ThenVRShouldBeFilled(string vr)
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(tag.GetPath(), vr, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready);
            ExtendedQueryTagEntry normalized = entry.Normalize(ExtendedQueryTagStatus.Adding);
            Assert.Equal(tag.GetDefaultVR().Code, normalized.VR);
        }

        [Fact]
        public void GivenStandardTagWithVR_WhenNormalizing_ThenVRShouldNotBeUpdated()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            string vr = DicomVR.CS.Code;
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(tag.GetPath(), vr, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready);
            ExtendedQueryTagEntry normalized = entry.Normalize(ExtendedQueryTagStatus.Adding);
            Assert.Equal(vr, normalized.VR);
        }

        [Fact]
        public void GivenTagOfLowerCase_WhenNormalizing_ThenTagShouldBeUpperCase()
        {
            DicomTag tag = DicomTag.DeviceLabel;
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(tag.GetPath().ToLowerInvariant(), tag.GetDefaultVR().Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready);
            ExtendedQueryTagEntry normalized = entry.Normalize(ExtendedQueryTagStatus.Adding);
            Assert.Equal(entry.Path.ToUpperInvariant(), normalized.Path);
        }

        [Fact]
        public void GivenVROfLowerCase_WhenNormalizing_ThenVRShouldBeUpperCase()
        {
            DicomTag tag = DicomTag.DeviceLabel;
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(tag.GetPath(), tag.GetDefaultVR().Code.ToLowerInvariant(), null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready);
            ExtendedQueryTagEntry normalized = entry.Normalize(ExtendedQueryTagStatus.Adding);
            Assert.Equal(entry.VR.ToUpperInvariant(), normalized.VR);
        }

        [Fact]

        public void GivenStandardTagAsKeyword_WhenNormalizing_ThenVRShouldBeFilled()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            ExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(path: tag.DictionaryEntry.Keyword, tag.GetDefaultVR().Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready);
            string expectedPath = tag.GetPath();
            ExtendedQueryTagEntry normalized = entry.Normalize(ExtendedQueryTagStatus.Adding);
            Assert.Equal(normalized.Path, expectedPath);
        }

        public static IEnumerable<object[]> GetValidExtendedQueryTagEntries()
        {
            yield return new object[] { DicomTag.DeviceSerialNumber.BuildExtendedQueryTagEntry() }; // standard extended query tag with VR
            yield return new object[] { CreateExtendedQueryTagEntry("12051003", DicomVRCode.OB, "PrivateCreator1", QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready) }; // private tag with VR
        }

        private static ExtendedQueryTagEntry CreateExtendedQueryTagEntry(string path, string vr, string privateCreator, QueryTagLevel level = QueryTagLevel.Instance, ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Ready)
        {
            return new ExtendedQueryTagEntry { Path = path, VR = vr, PrivateCreator = privateCreator, Level = level, Status = status };
        }
    }
}
