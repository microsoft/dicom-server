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
#if NET7_0_OR_GREATER
    [GeneratedRegex("^(0|([1-9][0-9]*))(\\\\.(0|([1-9][0-9]*)))*$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
    private static partial Regex UidRegexStrict();

    [GeneratedRegex("^[0-9\\.]*[0-9]$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
    private static partial Regex UidRegex();
#else
    private static readonly Regex UidRegexStrict = new("^(0|([1-9][0-9]*))(\\\\\\\\.(0|([1-9][0-9]*)))*$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
    private static readonly Regex UidRegex = new("^[0-9\\.]*[0-9]$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
#endif

    protected override void ValidateStringElement(string name, DicomVR vr, string value, IByteBuffer buffer, ValidationLevel validationLevel)
        => Validate(value, name, allowEmpty: true, validationLevel: validationLevel);

    public static bool IsValid(string value, bool allowEmpty = false, ValidationLevel validationLevel = ValidationLevel.Default)
    {
        if (string.IsNullOrEmpty(value))
            return allowEmpty;

        // trailing spaces are allowed
        value = value.TrimEnd(' ');

        if (value.Length > 64)
        {
            // UI value is validated in other cases like params for WADO, DELETE. So keeping the exception specific.
            return false;
        }

#if NET7_0_OR_GREATER
        if (validationLevel is ValidationLevel.Strict)
        {
            return UidRegexStrict().IsMatch(value);
        }

        return UidRegex().IsMatch(value);

#else
        if (validationLevel is ValidationLevel.Strict)
        {
            return UidRegexStrict.IsMatch(value);
        }

        return UidRegex.IsMatch(value);
#endif
    }

    public static void Validate(string value, string name, bool allowEmpty = false, ValidationLevel validationLevel = ValidationLevel.Default)
    {
        // UI value is validated in other cases like params for WADO, DELETE. So keeping the exception specific.
        if (!IsValid(value, allowEmpty, validationLevel))
            throw new InvalidIdentifierException(name);
    }
}
