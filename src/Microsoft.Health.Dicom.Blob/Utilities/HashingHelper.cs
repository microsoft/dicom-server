// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO.Hashing;

namespace Microsoft.Health.Dicom.Blob.Utilities;

internal static class HashingHelper
{
    /// <summary>
    /// Gets a non-cryptographic hash for a given value.
    /// </summary>
    /// <param name="value"> Value to be hashed</param>
    /// <returns>Returns hashed value as string. If hashLength is provided, it will return the value to that length</returns>
    public static int GetXxHashCode(long value)
    {
        unsafe
        {
            int hash;
            XxHash32.Hash(new Span<byte>(&value, 8), new Span<byte>(&hash, 4));
            return hash;
        }
    }
}
