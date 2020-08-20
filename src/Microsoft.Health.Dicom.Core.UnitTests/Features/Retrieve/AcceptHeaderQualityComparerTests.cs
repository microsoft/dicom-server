// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class AcceptHeaderQualityComparerTests
    {
        [Theory]
        [MemberData(nameof(GetTestData))]
        public void Given2AcceptHeader_WhenCompare_ThenShouldSucceed(AcceptHeader x, AcceptHeader y, int expected)
        {
            Assert.Equal(expected, new AcceptHeaderQualityComparer().Compare(x, y));
        }

        public static IEnumerable<object[]> GetTestData()
        {
            // x is null & y is null
            yield return new object[] { null, null, 0 };

            // x is null
            yield return new object[] { null, AcceptHeaderHelpers.CreateAcceptHeader(), -1 };

            // y is null
            yield return new object[] { AcceptHeaderHelpers.CreateAcceptHeader(), null, 1 };

            // x.quality is null & y.quality is null
            yield return new object[] { AcceptHeaderHelpers.CreateAcceptHeader(quality: null), AcceptHeaderHelpers.CreateAcceptHeader(quality: null), 0 };

            // x.quality is null
            yield return new object[] { AcceptHeaderHelpers.CreateAcceptHeader(quality: null), AcceptHeaderHelpers.CreateAcceptHeader(quality: 0.5), 1 };
            yield return new object[] { AcceptHeaderHelpers.CreateAcceptHeader(quality: null), AcceptHeaderHelpers.CreateAcceptHeader(quality: 1), 0 };

            // y.quality is null
            yield return new object[] { AcceptHeaderHelpers.CreateAcceptHeader(quality: 0.5), AcceptHeaderHelpers.CreateAcceptHeader(quality: null), -1 };
            yield return new object[] { AcceptHeaderHelpers.CreateAcceptHeader(quality: 1), AcceptHeaderHelpers.CreateAcceptHeader(quality: null), 0 };

            // x.quality>y.quality
            yield return new object[] { AcceptHeaderHelpers.CreateAcceptHeader(quality: 0.6), AcceptHeaderHelpers.CreateAcceptHeader(quality: 0.5), 1 };

            // x.quality<y.quality
            yield return new object[] { AcceptHeaderHelpers.CreateAcceptHeader(quality: 0.5), AcceptHeaderHelpers.CreateAcceptHeader(quality: 0.6), -1 };

            // x.quality=y.quality
            yield return new object[] { AcceptHeaderHelpers.CreateAcceptHeader(quality: 0), AcceptHeaderHelpers.CreateAcceptHeader(quality: 0), 0 };
        }
    }
}
