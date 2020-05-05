// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    /// <summary>
    /// Validates a unique identifier conforms to the character rules from
    /// http://dicom.nema.org/dicom/2013/output/chtml/part05/chapter_9.html.
    /// http://dicom.nema.org/dicom/2013/output/chtml/part05/sect_6.2.html
    /// </summary>
    public static class IdentifierValidator
    {
        private static readonly Regex ValidIdentifierFormat = new Regex("^[0-9\\.]*$", RegexOptions.Compiled);
        private static readonly Regex InvalidComponentStart = new Regex(@"[.]0\d", RegexOptions.Compiled);

        public static void ValidateAndThrow(string identifierValue, string parameterName)
        {
            if (!Validate(identifierValue))
            {
                throw new InvalidIdentifierException(identifierValue, parameterName);
            }
        }

        public static bool Validate(string identifierValue)
        {
            /*
             The UID is a series of numeric components separated by the period "." character.
             If a Value Field containing one or more UIDs is an odd number of bytes in length,
             the Value Field shall be padded with a single trailing NULL (00H)
             character to ensure that the Value Field is an even number of bytes in length.
             See Section 9 and Annex B for a complete specification and examples.
             "0"-"9", "." of Default Character Repertoire
             64 bytes maximum */

            if (identifierValue == null)
            {
                return false;
            }

            // trailing spaces are allowed
            identifierValue = identifierValue.TrimEnd(' ');
            if (string.IsNullOrEmpty(identifierValue))
            {
                // empty values are valid
                return true;
            }

            if (identifierValue.Length > 64)
            {
                return false;
            }

            if (!ValidIdentifierFormat.IsMatch(identifierValue))
            {
                return false;
            }

            if (identifierValue.StartsWith("0", System.StringComparison.OrdinalIgnoreCase) || InvalidComponentStart.IsMatch(identifierValue))
            {
                return false;
            }

            return true;
        }
    }
}
