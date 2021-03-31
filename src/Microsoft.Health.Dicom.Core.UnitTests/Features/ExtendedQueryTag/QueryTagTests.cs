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
    public class QueryTagTests
    {
        [Fact]
        public void GivenCoreDicomTag_WhenInitialize_ThenShouldCreatedSuccessfully()
        {
            DicomTag tag = DicomTag.PatientName;
            QueryTag queryTag = new QueryTag(tag);
            Assert.Equal(tag, queryTag.Tag);
            Assert.Equal(DicomVR.PN, queryTag.VR);
            Assert.Null(queryTag.ExtendedQueryTagStoreEntry);
            Assert.False(queryTag.IsExtendedQueryTag);
            Assert.Equal(QueryTagLevel.Study, queryTag.Level);
        }

        [Fact]
        public void GivenStandardExtendedQueryTag_WhenInitialize_ThenShouldCreatedSuccessfully()
        {
            DicomTag tag = DicomTag.AcquisitionDate;
            QueryTagLevel level = QueryTagLevel.Series;
            var storeEntry = tag.BuildExtendedQueryTagStoreEntry(level: level);
            QueryTag queryTag = new QueryTag(storeEntry);
            Assert.Equal(tag, queryTag.Tag);
            Assert.Equal(DicomVR.DA, queryTag.VR);
            Assert.Equal(storeEntry, queryTag.ExtendedQueryTagStoreEntry);
            Assert.True(queryTag.IsExtendedQueryTag);
            Assert.Equal(level, queryTag.Level);
        }

        [Fact]
        public void GivenPrivateExtendedQueryTag_WhenInitialize_ThenShouldCreatedSuccessfully()
        {
            DicomTag tag = new DicomTag(0x1205, 0x1003, "PrivateCreator1");
            DicomVR vr = DicomVR.CS;
            QueryTagLevel level = QueryTagLevel.Study;
            var storeEntry = tag.BuildExtendedQueryTagStoreEntry(vr: vr.ToString(), privateCreator: tag.PrivateCreator.Creator, level: level);
            QueryTag queryTag = new QueryTag(storeEntry);
            Assert.Equal(tag, queryTag.Tag);
            Assert.Equal(vr, queryTag.VR);
            Assert.Equal(storeEntry, queryTag.ExtendedQueryTagStoreEntry);
            Assert.True(queryTag.IsExtendedQueryTag);
            Assert.Equal(level, queryTag.Level);
        }
    }
}
