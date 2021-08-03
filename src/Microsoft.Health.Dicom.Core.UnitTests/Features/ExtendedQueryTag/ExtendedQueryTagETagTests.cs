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
    public class ExtendedQueryTagETagTests
    {
        [Fact]
        public void GivenEmptyTagVersionCollection_WhenGetETag_ShouldReturnNull()
        {
            Assert.Null(new ExtendedQueryTagETag(Array.Empty<ExtendedQueryTagVersion>()).ETag);
        }

        [Fact]
        public void GivenNonEmptyTagVersionCollection_WhenGetETag_ShouldReturnMax()
        {
            ExtendedQueryTagVersion version1 = new ExtendedQueryTagVersion(BitConverter.GetBytes(2L));
            ExtendedQueryTagVersion version2 = new ExtendedQueryTagVersion(BitConverter.GetBytes(1L));

            Assert.Equal(version1, new ExtendedQueryTagETag(new[] { version1, version2 }).ETag);
        }

        [Fact]
        public void GivenMultipleQueryTags_WhenCallFromQueryTags_ShouldReturnMax()
        {
            QueryTag queryTag1 = new QueryTag(DicomTag.PatientName); // Core Tag
            DicomTag tag2 = DicomTag.DeviceID;
            ExtendedQueryTagStoreEntry storeEntry2 = new ExtendedQueryTagStoreEntry(1, tag2.GetPath(), tag2.GetDefaultVR().Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, new ExtendedQueryTagVersion(BitConverter.GetBytes(1L)));
            QueryTag queryTag2 = new QueryTag(storeEntry2); // custom tag with smaller version
            DicomTag tag3 = DicomTag.AbortReason;
            ExtendedQueryTagStoreEntry storeEntry3 = new ExtendedQueryTagStoreEntry(1, tag3.GetPath(), tag3.GetDefaultVR().Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, new ExtendedQueryTagVersion(BitConverter.GetBytes(2L)));
            QueryTag queryTag3 = new QueryTag(storeEntry3); // custom tag with bigger version
            Assert.Equal(queryTag3.ExtendedQueryTagStoreEntry.Version, ExtendedQueryTagETag.FromQueryTags(new[] { queryTag1, queryTag2, queryTag3 }).ETag);
        }
    }
}
