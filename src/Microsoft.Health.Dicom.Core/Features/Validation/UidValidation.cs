// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

internal class UidValidation : StringElementValidation
{
    private static readonly Regex ValidIdentifierCharactersFormat =
        new Regex("^[0-9\\.]*[0-9]$", RegexOptions.Compiled);

    protected override void ValidateStringElement(string name, DicomVR vr, string value, IByteBuffer buffer)
    {
        Validate(value, name, allowEmpty: true);
    }

    public static bool IsValid(string value, bool allowEmpty = false)
    {
        if (string.IsNullOrEmpty(value))
        {
            return allowEmpty;
        }

        // trailling spaces are allowed
        value = value.TrimEnd(' ');

        if (value.Length > 64)
        {
            // UI value is validated in other cases like params for WADO, DELETE. So keeping the exception specific.
            return false;
        }
        else if (!ValidIdentifierCharactersFormat.IsMatch(value))
        {
            return false;
        }

        return true;
    }

    public static void Validate(string value, string name, bool allowEmpty = false)
    {
        if (!IsValid(value, allowEmpty))
        {
            throw new InvalidIdentifierException(name);
        }
    }
}
