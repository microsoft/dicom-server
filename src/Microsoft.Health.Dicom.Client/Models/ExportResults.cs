// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Client.Models;

/// <summary>
/// Represents the current state of the export operation.
/// </summary>
public sealed class ExportResults
{
    /// <summary>
    /// Gets or sets the URI for containing the errors for this operation, if any.
    /// </summary>
    /// <value>
    /// The <see cref="Uri"/> for the resource containg export errors.
    /// </value>
    public Uri ErrorHref { get; set; }

    /// <summary>
    /// Gets the number of DICOM files that were successfully exported.
    /// </summary>
    /// <value>The non-negative number of exported DICOM files.</value>
    public long Exported { get; set; }

    /// <summary>
    /// Gets the number of DICOM files that were skipped because they failed to be exported.
    /// </summary>
    /// <value>The non-negative number of DICOM files that failed to be exported.</value>
    public long Skipped { get; set; }
}
