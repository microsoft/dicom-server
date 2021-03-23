// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag
{
    public class IndexTagTests
    {
        [Fact]
        public void GivenCoreDicomTag_WhenInitialize_ThenShouldCreatedSuccessfully()
        {
            DicomTag tag = DicomTag.PatientName;
            QueryTagLevel level = QueryTagLevel.Instance;
            QueryTag indexTag = new QueryTag(tag, QueryTagLevel.Instance);
            Assert.Equal(tag, indexTag.Tag);
            Assert.Equal(DicomVR.PN, indexTag.VR);
            Assert.Null(indexTag.ExtendedQueryTagStoreEntry);
            Assert.False(indexTag.IsExtendedQueryTag);
            Assert.Equal(level, indexTag.Level);
        }

        [Fact]
        public void GivenStandardExtendedQueryTag_WhenInitialize_ThenShouldCreatedSuccessfully()
        {
            DicomTag tag = DicomTag.AcquisitionDate;
            QueryTagLevel level = QueryTagLevel.Series;
            var storeEntry = tag.BuildExtendedQueryTagStoreEntry(level: level);
            QueryTag indexTag = new QueryTag(storeEntry);
            Assert.Equal(tag, indexTag.Tag);
            Assert.Equal(DicomVR.DA, indexTag.VR);
            Assert.Equal(storeEntry, indexTag.ExtendedQueryTagStoreEntry);
            Assert.True(indexTag.IsExtendedQueryTag);
            Assert.Equal(level, indexTag.Level);
        }

        [Fact]
        public void GivenPrivateExtendedQueryTag_WhenInitialize_ThenShouldCreatedSuccessfully()
        {
            DicomTag tag = new DicomTag(0x1205, 0x1003, "PrivateCreator1");
            DicomVR vr = DicomVR.CS;
            QueryTagLevel level = QueryTagLevel.Study;
            var storeEntry = tag.BuildExtendedQueryTagStoreEntry(vr: vr.ToString(), privateCreator: tag.PrivateCreator.Creator, level: level);
            QueryTag indexTag = new QueryTag(storeEntry);
            Assert.Equal(tag, indexTag.Tag);
            Assert.Equal(vr, indexTag.VR);
            Assert.Equal(storeEntry, indexTag.ExtendedQueryTagStoreEntry);
            Assert.True(indexTag.IsExtendedQueryTag);
            Assert.Equal(level, indexTag.Level);
        }
    }
}
