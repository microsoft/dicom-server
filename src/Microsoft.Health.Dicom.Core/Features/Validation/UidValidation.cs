// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

internal class UidValidation : IElementValidation
{
    private static readonly Regex ValidIdentifierCharactersFormat = new Regex("^[0-9\\.]*[0-9]$", RegexOptions.Compiled);

    public void Validate(DicomElement dicomElement, bool withLeniency = false)
    {
        string value = dicomElement.GetFirstValueOrDefault<string>();
        if (withLeniency)
        {
            value = value.TrimEnd('\0');
        }
        string name = dicomElement.Tag.GetFriendlyName();
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
