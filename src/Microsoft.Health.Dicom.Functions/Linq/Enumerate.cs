// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Functions.Linq;

internal static class Enumerate
{
    public static IEnumerable<long> Range(long start, int count)
        => Range(start, count, 1);

    public static IEnumerable<long> Range(long start, int count, long step)
    {
        for (int i = 0; i < count; i++)
        {
            yield return start;
            start += step;
        }
    }
}
