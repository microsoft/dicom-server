// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

internal partial class UidValidation : StringElementValidation
{
    // Note: This regex allows for leading zeroes.
    // A more strict regex may look like: "^(0|([1-9][0-9]*))(\\.(0|([1-9][0-9]*)))*$"
#if NET7_0_OR_GREATER
    [GeneratedRegex("^[0-9\\.]*[0-9]$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
    private static partial Regex UidRegex();
#else
    private static readonly Regex UidRegex = new("^[0-9\\.]*[0-9]$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
#endif

    protected override void ValidateStringElement(string name, DicomVR vr, string value, IByteBuffer buffer)
        => Validate(value, name, allowEmpty: true);

    public static bool IsValid(string value, bool allowEmpty = false)
    {
        if (string.IsNullOrEmpty(value))
            return allowEmpty;

        // trailling spaces are allowed
        value = value.TrimEnd(' ');

#if NET7_0_OR_GREATER
        if (value.Length > 64 || !UidRegex().IsMatch(value))
#else
        if (value.Length > 64 || !UidRegex.IsMatch(value))
#endif
            return false;

        return true;
    }

    public static void Validate(string value, string name, bool allowEmpty = false)
    {
        // UI value is validated in other cases like params for WADO, DELETE. So keeping the exception specific.
        if (!IsValid(value, allowEmpty))
            throw new InvalidIdentifierException(name);
    }
}
