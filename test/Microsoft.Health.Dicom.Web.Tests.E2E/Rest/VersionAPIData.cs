// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class VersionAPIData : IEnumerable<object[]>
    {
        public static IReadOnlyList<object[]> VersionSegmentData { get; } = new List<object[]>
        {
            new object[] { "" },
            new object[] { "v1.0-prerelease" }
        };

        public IEnumerator<object[]> GetEnumerator() => VersionSegmentData.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
