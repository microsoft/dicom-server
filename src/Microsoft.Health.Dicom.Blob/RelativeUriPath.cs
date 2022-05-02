// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Blob;

internal static class RelativeUriPath
{
    public static string Combine(string first, string second)
    {
        EnsureArg.IsNotNull(first, nameof(first));
        EnsureArg.IsNotNull(second, nameof(second));

        if (first.Length == 0)
            return second;

        if (second.Length == 0)
            return first;

        if (first[^1] == '/')
        {
            return second[0] == '/' ? string.Concat(first, second.AsSpan(1)) : string.Concat(first, second);
        }
        else
        {
            return second[0] == '/' ? string.Concat(first, second) : string.Concat(first, "/", second);
        }
    }
}
