// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using FellowOakDicom;
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
        public void GivenValidExtendedQueryTagEntry_WhenNormalizing_ThenShouldReturnSameEntry(AddExtendedQueryTagEntry entry)
        {
            AddExtendedQueryTagEntry normalized = entry.Normalize();
            Assert.Equal(entry.Path, normalized.Path);
            Assert.Equal(entry.VR, normalized.VR);
            Assert.Equal(entry.Level, normalized.Level);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void GivenPrivateTagWithNonEmptyPrivateCreator_WhenNormalizing_ThenPrivateCreatorShouldBeNull(string privateCreator)
        {
            DicomTag tag1 = new DicomTag(0x0405, 0x1001);
            AddExtendedQueryTagEntry normalized = new AddExtendedQueryTagEntry() { Level = QueryTagLevel.Instance.ToString(), Path = tag1.GetPath(), PrivateCreator = privateCreator, VR = DicomVRCode.CS }.Normalize();
            Assert.Null(normalized.PrivateCreator);
        }

        [Fact]

        public void GivenPrivateIdentificationCodeTagWithoutVR_WhenNormalizing_ThenVRShouldBeFilled()
        {
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry("22010010", null, null, QueryTagLevel.Instance);
            AddExtendedQueryTagEntry normalized = entry.Normalize();
            Assert.Equal(DicomVRCode.LO, normalized.VR);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]

        public void GivenStandardTagWithoutVR_WhenNormalizing_ThenVRShouldBeFilled(string vr)
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(tag.GetPath(), vr, null, QueryTagLevel.Instance);
            AddExtendedQueryTagEntry normalized = entry.Normalize();
            Assert.Equal(tag.GetDefaultVR().Code, normalized.VR);
        }

        [Fact]
        public void GivenStandardTagWithVR_WhenNormalizing_ThenVRShouldNotBeUpdated()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            string vr = DicomVR.CS.Code;
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(tag.GetPath(), vr, null, QueryTagLevel.Instance);
            AddExtendedQueryTagEntry normalized = entry.Normalize();
            Assert.Equal(vr, normalized.VR);
        }

        [Fact]
        public void GivenTagOfLowerCase_WhenNormalizing_ThenTagShouldBeUpperCase()
        {
            DicomTag tag = DicomTag.DeviceLabel;
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(tag.GetPath().ToLowerInvariant(), tag.GetDefaultVR().Code, null, QueryTagLevel.Instance);
            AddExtendedQueryTagEntry normalized = entry.Normalize();
            Assert.Equal(entry.Path.ToUpperInvariant(), normalized.Path);
        }

        [Fact]
        public void GivenVROfLowerCase_WhenNormalizing_ThenVRShouldBeUpperCase()
        {
            DicomTag tag = DicomTag.DeviceLabel;
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(tag.GetPath(), tag.GetDefaultVR().Code.ToLowerInvariant(), null, QueryTagLevel.Instance);
            AddExtendedQueryTagEntry normalized = entry.Normalize();
            Assert.Equal(entry.VR.ToUpperInvariant(), normalized.VR);
        }

        [Fact]

        public void GivenStandardTagAsKeyword_WhenNormalizing_ThenVRShouldBeFilled()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(path: tag.DictionaryEntry.Keyword, tag.GetDefaultVR().Code, null, QueryTagLevel.Instance);
            string expectedPath = tag.GetPath();
            AddExtendedQueryTagEntry normalized = entry.Normalize();
            Assert.Equal(normalized.Path, expectedPath);
        }

        [Fact]

        public void GivenInvalidTagWithoutVR_WhenNormalizing_ThenShouldNotThrowException()
        {
            // Add this unit test for regression: we had a bug when tag is valid and VR is null, NullPointerException is thrown. More details can be found https://microsofthealth.visualstudio.com/Health/_workitems/edit/81015
            AddExtendedQueryTagEntry entry = CreateExtendedQueryTagEntry(path: "00111011", null, null, QueryTagLevel.Series);
            entry.Normalize();
        }

        public static IEnumerable<object[]> GetValidExtendedQueryTagEntries()
        {
            yield return new object[] { DicomTag.DeviceSerialNumber.BuildAddExtendedQueryTagEntry() }; // standard extended query tag with VR
            yield return new object[] { CreateExtendedQueryTagEntry("12051003", DicomVRCode.OB, "PrivateCreator1", QueryTagLevel.Instance) }; // private tag with VR            
        }

        private static GetExtendedQueryTagEntry CreateExtendedQueryTagEntry(string path, string vr, string privateCreator, QueryTagLevel level = QueryTagLevel.Instance, ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Ready)
        {
            return new GetExtendedQueryTagEntry { Path = path, VR = vr, PrivateCreator = privateCreator, Level = level, Status = status };
        }

        private static AddExtendedQueryTagEntry CreateExtendedQueryTagEntry(string path, string vr, string privateCreator, QueryTagLevel level = QueryTagLevel.Instance)
        {
            return new AddExtendedQueryTagEntry { Path = path, VR = vr, PrivateCreator = privateCreator, Level = level.ToString() };
        }
    }
}
