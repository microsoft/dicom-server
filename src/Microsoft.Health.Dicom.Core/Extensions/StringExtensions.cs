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

            // truncate by replace addtional characters with up to 3 dots.
            int dotCount = maxLength >= 3 ? 3 : maxLength;
            int textRemainCount = maxLength - dotCount;
            return text.Substring(0, textRemainCount) + new string('.', dotCount);
        }
    }
}
