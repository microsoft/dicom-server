// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag
{
    public class IndexTagTests
    {
        [Fact]
        public void GivenCoreDicomTag_WhenInitialize_ThenShouldCreatedSuccessfully()
        {
            DicomTag tag = DicomTag.PatientName;
            CustomTagLevel level = CustomTagLevel.Instance;
            IndexTag indexTag = new IndexTag(tag, CustomTagLevel.Instance);
            Assert.Equal(tag, indexTag.Tag);
            Assert.Equal(DicomVR.PN, indexTag.VR);
            Assert.Null(indexTag.CustomTagStoreEntry);
            Assert.False(indexTag.IsCustomTag);
            Assert.Equal(level, indexTag.Level);
        }

        [Fact]
        public void GivenStandardCustomTag_WhenInitialize_ThenShouldCreatedSuccessfully()
        {
            DicomTag tag = DicomTag.AcquisitionDate;
            CustomTagLevel level = CustomTagLevel.Series;
            var storeEntry = tag.BuildCustomTagStoreEntry(level: level);
            IndexTag indexTag = new IndexTag(storeEntry);
            Assert.Equal(tag, indexTag.Tag);
            Assert.Equal(DicomVR.DA, indexTag.VR);
            Assert.Equal(storeEntry, indexTag.CustomTagStoreEntry);
            Assert.True(indexTag.IsCustomTag);
            Assert.Equal(level, indexTag.Level);
        }

        [Fact]
        public void GivenPrivateCustomTag_WhenInitialize_ThenShouldCreatedSuccessfully()
        {
            DicomTag tag = new DicomTag(0x1205, 0x1003, "PrivateCreator1");
            DicomVR vr = DicomVR.CS;
            CustomTagLevel level = CustomTagLevel.Study;
            var storeEntry = tag.BuildCustomTagStoreEntry(vr: vr.ToString(), privateCreator: tag.PrivateCreator.Creator, level: level);
            IndexTag indexTag = new IndexTag(storeEntry);
            Assert.Equal(tag, indexTag.Tag);
            Assert.Equal(vr, indexTag.VR);
            Assert.Equal(storeEntry, indexTag.CustomTagStoreEntry);
            Assert.True(indexTag.IsCustomTag);
            Assert.Equal(level, indexTag.Level);
        }
    }
}
