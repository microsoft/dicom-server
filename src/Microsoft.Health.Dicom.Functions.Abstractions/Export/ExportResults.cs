// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Functions.Export;

/// <summary>
/// Represents the current state of the export operation to end-users.
/// </summary>
public sealed class ExportResults
{
    /// <summary>
    /// Gets the number of DICOM files that were successfully exported.
    /// </summary>
    /// <value>The non-negative number of exported DICOM files.</value>
    public long Exported { get; }

    /// <summary>
    /// Gets the number of DICOM resources that were skipped because they failed to be exported.
    /// </summary>
    /// <value>The non-negative number of DICOM resources that failed to be exported.</value>
    public long Skipped { get; }

    /// <summary>
    /// Gets the URI for containing the errors for this operation, if any.
    /// </summary>
    /// <value>The <see cref="Uri"/> for the resource containg export errors.</value>
    public Uri ErrorHref { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportResults"/> structure based given progress.
    /// </summary>
    /// <param name="progress">The progress made by the export operation so far.</param>
    /// <param name="errorHref">The URI for the error log.</param>
    public ExportResults(ExportProgress progress, Uri errorHref)
    {
        Exported = progress.Exported;
        Skipped = progress.Skipped;
        ErrorHref = errorHref;
    }
}
