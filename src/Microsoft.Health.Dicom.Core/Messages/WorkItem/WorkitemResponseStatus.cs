// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Messages.Workitem;

/// <summary>
/// Represents the add work-item response status.
/// </summary>
public enum WorkitemResponseStatus
{
    /// <summary>
    /// There is no DICOM instance to add.
    /// </summary>
    None,

    /// <summary>
    /// The DICOM work-item instance is not found
    /// </summary>
    NotFound,

    /// <summary>
    /// All DICOM work-item instance(s) have been add successfully.
    /// </summary>
    Success,

    /// <summary>
    /// All DICOM work-item instance(s) have failed to add.
    /// </summary>
    Failure,

    /// <summary>
    /// Workitem instance already exist.
    /// </summary>
    Conflict,

    /// <summary>
    /// All matching workitem instance(s) found
    /// </summary>
    NoContent,

    /// <summary>
    /// Only partial workitem instance(s) found
    /// </summary>
    PartialContent,
}
