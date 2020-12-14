// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag
{
    public class CustomTagListTests
    {
        [Fact]
        public void GivenEmptyCustomTags_WhenConstructCustomTagList_ShouldVersionBeNull()
        {
            CustomTagList customTagList = new CustomTagList(new List<CustomTagEntry>());
            Assert.Null(customTagList.Version);
        }

        [Fact]
        public void GivenNonEmptyCustomTags_WhenConstructCustomTagList_ShouldVersionBeBiggestOne()
        {
            List<CustomTagEntry> list = new List<CustomTagEntry>() { CustomTagTestHelper.CreateCustomTagEntry(version: 1), CustomTagTestHelper.CreateCustomTagEntry(version: 2) };

            CustomTagList customTagList = new CustomTagList(list);
            Assert.Equal(2, customTagList.Version);
        }
    }
}
