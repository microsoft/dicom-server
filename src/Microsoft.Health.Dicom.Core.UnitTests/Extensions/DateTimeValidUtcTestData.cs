// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions
{
    public class DateTimeValidUtcTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "20200301010203.123+0500", 2020, 02, 29, 20, 02, 03, 123 };
            yield return new object[] { "20200301010203.123-0500", 2020, 03, 01, 06, 02, 03, 123 };
            yield return new object[] { "20200301010203-0500", 2020, 03, 01, 06, 02, 03, 0 };
            yield return new object[] { "202003010102-0500", 2020, 03, 01, 06, 02, 0, 0 };
            yield return new object[] { "2020030101-0500", 2020, 03, 01, 06, 0, 0, 0 };
            yield return new object[] { "20200301-0500", 2020, 03, 01, 05, 0, 0, 0 };
            yield return new object[] { "202003-0500", 2020, 03, 01, 05, 0, 0, 0 };
            yield return new object[] { "2020-0500", 2020, 01, 01, 05, 0, 0, 0 };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
