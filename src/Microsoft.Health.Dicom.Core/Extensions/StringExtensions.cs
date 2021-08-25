// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="string"/>.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Truncate text to given length.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="length">The length to truncate</param>
        /// <param name="result">Truncated text.</param>
        /// <returns>True if truncated, false otherwise.</returns>
        public static bool TryTruncate(this string text, int length, out string result)
        {
            result = text;
            EnsureArg.IsNotNull(text, nameof(text));
            EnsureArg.IsGte(length, 0, nameof(length));
            if (text.Length <= length)
            {
                result = text;
                return false;
            }
            if (length == 0)
            {
                result = string.Empty;
                return true;
            }

            // Truncate last character if it is high surrogate char to promise readable text.
            result = text.Substring(0, char.IsHighSurrogate(text[length - 1]) ? length - 1 : length);
            return true;
        }

        /// <summary>
        /// Truncate text to given length.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="length">The length to truncate</param>
        /// <returns>The truncated text.</returns>
        public static string Truncate(this string text, int length)
        {
            TryTruncate(text, length, out string result);
            return result;
        }
    }
}
