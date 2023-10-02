// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Validation;

/// <summary>
/// A set of validation levels to pick from to run validations against
/// </summary>
public enum ValidationLevel
{
    /// <summary>
    /// Default takes a more lenient approach with strings when running validation rules. It drops null padding on a string and then continues to
    /// perform all validation after.
    /// </summary>
    Default,

    /// <summary>
    /// Strict validation currently fails when a string value has null padding
    /// </summary>
    Strict
}