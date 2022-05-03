// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;
using EnsureThat;
using HashDepot;

namespace Microsoft.Health.Dicom.Blob.Utilities;
internal static class HashingHelper
{
    /// <summary>
    /// Gets a deterministic hash for a given value
    /// </summary>
    /// <param name="value"> Value to be hashed</param>
    /// <param name="hashLength">Length of the hash to be returned. -1 means the complete hash value without trimming.</param>
    /// <returns>Returns hashed value as string. If hashLength is provided, it will return the value to that length</returns>
    public static string Hash(long value, int hashLength = -1)
    {
        EnsureArg.IsNotDefault(value, nameof(value));

        byte[] buffer = Encoding.UTF8.GetBytes(value.ToString());
        var hash = XXHash.Hash64(buffer).ToString();

        // If the hashLength is greater than the hash, assigning the length of the hash and not throw error
        hashLength = hashLength > hash.Length ? hash.Length : hashLength;
        return hash.Substring(0, hashLength == -1 ? hash.Length : hashLength);
    }
}
