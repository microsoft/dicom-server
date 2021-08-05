// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag
{
    public class ExtendedQueryTagVersionTests
    {
        [Fact]
        public void GivenEqualTagVersion_WhenCompare_ShouldReturnZero()
        {
            ExtendedQueryTagVersion version1 = new ExtendedQueryTagVersion(BitConverter.GetBytes(1L));
            ExtendedQueryTagVersion version2 = new ExtendedQueryTagVersion(BitConverter.GetBytes(1L));
            Assert.Equal(version1, version2);
        }

        [Fact]
        public void GivenBiggerTagVersion_WhenCompare_ShouldReturnPositive()
        {
            ExtendedQueryTagVersion version1 = new ExtendedQueryTagVersion(BitConverter.GetBytes(2L));
            ExtendedQueryTagVersion version2 = new ExtendedQueryTagVersion(BitConverter.GetBytes(1L));
            Assert.True(version1 > version2);
        }

        [Fact]
        public void GivenSmallerTagVersion_WhenCompare_ShouldReturnNegative()
        {
            ExtendedQueryTagVersion version1 = new ExtendedQueryTagVersion(BitConverter.GetBytes(1L));
            ExtendedQueryTagVersion version2 = new ExtendedQueryTagVersion(BitConverter.GetBytes(2L));
            Assert.True(version1 < version2);
        }

        [Fact]
        public void GivenEmptyTagVersionCollection_WhenGetVersion_ShouldReturnNull()
        {
            Assert.Null(ExtendedQueryTagVersion.GetExtendedQueryTagVersion(Array.Empty<ExtendedQueryTagVersion>()));
        }

        [Fact]
        public void GivenNonEmptyTagVersionCollection_WhenGetVersion_ShouldReturnMax()
        {
            ExtendedQueryTagVersion version1 = new ExtendedQueryTagVersion(BitConverter.GetBytes(2L));
            ExtendedQueryTagVersion version2 = new ExtendedQueryTagVersion(BitConverter.GetBytes(1L));

            Assert.Equal(version1, ExtendedQueryTagVersion.GetExtendedQueryTagVersion(new[] { version1, version2 }));
        }

        [Fact]
        public void GivenMultipleQueryTags_WhenGetVersion_ShouldReturnMax()
        {
            QueryTag queryTag1 = new QueryTag(DicomTag.PatientName); // Core Tag
            DicomTag tag2 = DicomTag.DeviceID;
            ExtendedQueryTagStoreEntry storeEntry2 = new ExtendedQueryTagStoreEntry(1, tag2.GetPath(), tag2.GetDefaultVR().Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, new ExtendedQueryTagVersion(BitConverter.GetBytes(1L)));
            QueryTag queryTag2 = new QueryTag(storeEntry2); // custom tag with smaller version
            DicomTag tag3 = DicomTag.AbortReason;
            ExtendedQueryTagStoreEntry storeEntry3 = new ExtendedQueryTagStoreEntry(1, tag3.GetPath(), tag3.GetDefaultVR().Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, new ExtendedQueryTagVersion(BitConverter.GetBytes(2L)));
            QueryTag queryTag3 = new QueryTag(storeEntry3); // custom tag with bigger version
            Assert.Equal(queryTag3.ExtendedQueryTagStoreEntry.Version, ExtendedQueryTagVersion.GetExtendedQueryTagVersion(new[] { queryTag1, queryTag2, queryTag3 }));
        }
    }
}
