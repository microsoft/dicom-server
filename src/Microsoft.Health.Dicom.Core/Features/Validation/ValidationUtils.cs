// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Health.Dicom.Core.Features.Validation;
internal static class ValidationUtils
{
    /// <summary>
    /// Checks for the presence of a character that does not fit the defined Character Repertoire
    /// for VRs LO, PN, and SH: "Default Character Repertoire and/or as defined by (0008,0005) excluding
    /// character code 5CH (the BACKSLASH "\" in ISO-IR 6) and all Control Characters except ESC when
    /// used for [ISO/IEC 2022] escape sequences." <see href="https://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_6.2"/>
    /// We allow trailing null characters due to the confusion around null character padding.
    /// </summary>
    /// <param name="text">The string to check.</param>
    /// <returns>A boolean representing the validity of the characters.</returns>
    public static bool ContainsValidStringCharacters(string text)
        => text != null && text.TrimEnd('\0').All(c =>
            c != '\\'
            && (!char.IsControl(c)
                || (c == '\u001b')));

    /// <summary>
    /// Checks for the presence of a character that does not fit the defined Character Repertoire
    /// for VRs LT, ST, and UT: "Default Character Repertoire and/or as defined by (0008,0005) excluding
    /// Control Characters except TAB, LF, FF, CR (and ESC when used for [ISO/IEC 2022] escape sequences)."
    /// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_6.2"/>
    /// We allow trailing null characters due to the confusion around null character padding.
    /// </summary>
    /// <param name="text">The string to check.</param>
    /// <returns>A boolean representing the validity of the characters.</returns>
    public static bool ContainsValidTextStringCharacters(string text)
        => text != null && text.TrimEnd('\0').All(c =>
            !char.IsControl(c)
            || AllowedFormattingChars.Contains(c));

    /// <summary>
    /// Defined in https://dicom.nema.org/medical/dicom/current/output/html/part05.html#table_6.1-1
    /// </summary>
    private static readonly HashSet<char> AllowedFormattingChars = new HashSet<char>
    {
        '\u0009', // HT
        '\u000a', // LF
        '\u000c', // FF
        '\u000d', // CR
        '\u001b', // ESC
    };
}
