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
        /// Truncate text to max length.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="maxLength">The max length</param>
        /// <returns>The truncated text.</returns>
        public static string Truncate(this string text, int maxLength)
        {
            EnsureArg.IsNotNull(text, nameof(text));
            EnsureArg.IsGte(maxLength, 0, nameof(maxLength));
            if (text.Length <= maxLength)
            {
                return text;
            }
            if (maxLength == 0)
            {
                return string.Empty;
            }

            if (text.Length <= 3)
            {
                return text.Substring(0, maxLength);
            }

            // truncate by replace addtional characters with 3 dots.
            return text.Substring(0, text.Length - 3) + "...";
        }
    }
}
