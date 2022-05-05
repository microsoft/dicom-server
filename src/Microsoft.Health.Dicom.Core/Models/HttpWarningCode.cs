// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Models;

/// <summary>
/// Represents Http Warning code.  https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Warning#warning_codes
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "0 is not valid HttpWarningCode")]
public enum HttpWarningCode
{
    /// <summary>
    /// Miscellaneous Persistent Warning
    /// </summary>
    MiscPersistentWarning = 299
}
