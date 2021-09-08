// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Extensions
{
    public class ExtendedQueryTagStoreEntryExtensionsTests
    {
        [Fact]
        public void GivenNoEntries_WhenGetMaxVersion_ShouldReturnNull()
        {
            Assert.Null(Array.Empty<ExtendedQueryTagStoreEntry>().GetMaxTagVersion());
        }

        [Fact]
        public void GivenMultipleEntries_WhenGetMaxVersion_ShouldReturnMaxOne()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            ExtendedQueryTagStoreEntry entry0 = new ExtendedQueryTagStoreEntry(1, tag.GetPath(), tag.GetDefaultVR().Code, string.Empty, QueryTagLevel.Series, ExtendedQueryTagStatus.Ready, null, QueryTagQueryStatus.Enabled);
            ExtendedQueryTagStoreEntry entry1 = new ExtendedQueryTagStoreEntry(1, tag.GetPath(), tag.GetDefaultVR().Code, string.Empty, QueryTagLevel.Series, ExtendedQueryTagStatus.Ready, 1, QueryTagQueryStatus.Enabled);
            ExtendedQueryTagStoreEntry entry2 = new ExtendedQueryTagStoreEntry(1, tag.GetPath(), tag.GetDefaultVR().Code, string.Empty, QueryTagLevel.Series, ExtendedQueryTagStatus.Ready, 2, QueryTagQueryStatus.Enabled);
            Assert.Equal((ulong)2, new[] { entry0, entry1, entry2 }.GetMaxTagVersion());
        }

        [Fact]
        public void GivenMultipleEntriesWithoutTagVersion_WhenGetMaxVersion_ShouldReturnNull()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            ExtendedQueryTagStoreEntry entry0 = new ExtendedQueryTagStoreEntry(1, tag.GetPath(), tag.GetDefaultVR().Code, string.Empty, QueryTagLevel.Series, ExtendedQueryTagStatus.Ready, null, QueryTagQueryStatus.Enabled);
            ExtendedQueryTagStoreEntry entry1 = new ExtendedQueryTagStoreEntry(1, tag.GetPath(), tag.GetDefaultVR().Code, string.Empty, QueryTagLevel.Series, ExtendedQueryTagStatus.Ready, null, QueryTagQueryStatus.Enabled);

            Assert.Null(new[] { entry0, entry1 }.GetMaxTagVersion());
        }
    }
}
