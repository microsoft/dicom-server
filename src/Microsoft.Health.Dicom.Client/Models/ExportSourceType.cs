// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Client.Models;

/// <summary>
/// Specifies the kind of format used to describe the data set to be exported.
/// </summary>
public enum ExportSourceType
{
    /// <summary>
    /// Specifies an unknown source format.
    /// </summary>
    Unknown,

    /// <summary>
    /// Specifies a list of DICOM identifiers.
    /// </summary>
    Identifiers,
}
