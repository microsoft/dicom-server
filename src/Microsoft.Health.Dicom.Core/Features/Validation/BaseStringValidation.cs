// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Validation;

/// <summary>
/// 
/// </summary>
public static class BaseStringValidation
{
    /// <summary>
    /// Sanitizes string value to prep for validation
    /// </summary>
    public static string Sanitize(string value, ValidationStyle validationStyle)
    {
        return validationStyle == ValidationStyle.Default
            ? string.IsNullOrEmpty(value) ? value : RemoveNullPadding(value)
            : value;
    }

    private static string RemoveNullPadding(string value)
    {
        return value.TrimEnd('\0');
    }
}