// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
    }
}
