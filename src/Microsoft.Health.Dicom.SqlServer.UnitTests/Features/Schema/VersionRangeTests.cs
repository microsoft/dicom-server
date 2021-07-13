// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Schema
{
    public class VersionRangeTests
    {
        [Fact]
        public void GivenSingleVersion_WhenCreatingRange_ThenCreateSingleton()
        {
            VersionRange range;

            // ctor
            range = new VersionRange(SchemaVersion.V2);
            Assert.Equal(1, range.Count);
            Assert.Equal(SchemaVersion.V2, range.Min);
            Assert.Equal(SchemaVersion.V2, range.Max);
            Assert.Equal(SchemaVersion.V2, range.Single());

            // cast
            range = SchemaVersion.V3;
            Assert.Equal(1, range.Count);
            Assert.Equal(SchemaVersion.V3, range.Min);
            Assert.Equal(SchemaVersion.V3, range.Max);
            Assert.Equal(SchemaVersion.V3, range.Single());
        }

        [Theory]
        [InlineData(SchemaVersion.V1, SchemaVersion.V3)]
        [InlineData(SchemaVersion.V2, SchemaVersion.V2)]
        public void GivenMultipleVersions_WhenCreatingRange_ThenContainAllVersions(SchemaVersion min, SchemaVersion max)
        {
            var range = new VersionRange(min, max);
            Assert.Equal(max - min + 1, range.Count);
            Assert.Equal(min, range.Min);
            Assert.Equal(max, range.Max);
        }

        [Theory]
        [InlineData(SchemaVersion.V1, SchemaVersion.V3, SchemaVersion.V1, true)]
        [InlineData(SchemaVersion.V1, SchemaVersion.V3, SchemaVersion.V2, true)]
        [InlineData(SchemaVersion.V1, SchemaVersion.V3, SchemaVersion.V3, true)]
        [InlineData(SchemaVersion.V1, SchemaVersion.V3, SchemaVersion.V4, false)]
        [InlineData(SchemaVersion.V1, SchemaVersion.V3, SchemaVersion.Unknown, false)]
        [InlineData(SchemaVersion.V4, SchemaVersion.V4, SchemaVersion.V4, true)]
        [InlineData(SchemaVersion.V4, SchemaVersion.V4, SchemaVersion.V2, false)]
        public void GivenRange_WhenCheckingContains_ThenPerformCheck(
            SchemaVersion min,
            SchemaVersion max,
            SchemaVersion version,
            bool expected)
        {
            Assert.Equal(expected, new VersionRange(min, max).Contains(version));
        }

        [Theory]
        [InlineData(SchemaVersion.V1, SchemaVersion.V3, SchemaVersion.V1, SchemaVersion.V3, true)]
        [InlineData(SchemaVersion.V3, SchemaVersion.V3, SchemaVersion.V3, SchemaVersion.V3, true)]
        [InlineData(SchemaVersion.V1, SchemaVersion.V3, SchemaVersion.V1, SchemaVersion.V4, false)]
        [InlineData(SchemaVersion.V1, SchemaVersion.V3, SchemaVersion.V2, SchemaVersion.V3, false)]
        [InlineData(SchemaVersion.V1, SchemaVersion.V2, SchemaVersion.V3, SchemaVersion.V4, false)]
        public void GivenRange_WhenCheckingEquality_ThenCheckEnds(
            SchemaVersion leftMin,
            SchemaVersion leftMax,
            SchemaVersion rightMin,
            SchemaVersion rightMax,
            bool expected)
        {
            var left = new VersionRange(leftMin, leftMax);
            var right = new VersionRange(rightMin, rightMax);

            Assert.Equal(expected, left.Equals(right));
            Assert.Equal(expected, left.Equals((object)right));
            Assert.Equal(expected, left == right);
            Assert.Equal(!expected, left != right);
        }

        [Fact]
        public void GivenRange_WhenEnumeratoring_ThenVerifyAllElements()
        {
            var range = new VersionRange(SchemaVersion.V1, SchemaVersion.V3);
            Assert.Equal(3, range.Count);

            var actual = range.ToList();
            Assert.Equal(3, actual.Count);
            Assert.Equal(SchemaVersion.V1, actual[0]);
            Assert.Equal(SchemaVersion.V2, actual[1]);
            Assert.Equal(SchemaVersion.V3, actual[2]);
        }
    }
}
