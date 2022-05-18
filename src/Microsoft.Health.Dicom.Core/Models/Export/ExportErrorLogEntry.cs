// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Models.Common;

namespace Microsoft.Health.Dicom.Core.Models.Export;

/// <summary>
/// Represents an entry in the export error log.
/// </summary>
public sealed class ExportErrorLogEntry
{
    /// <summary>
    /// Gets or sets the timestamp that the error ocurred.
    /// </summary>
    /// <value>The date and time of the error.</value>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the identifier for the study, series, or SOP instance that failed to export.
    /// </summary>
    /// <value>The identifier for the failed DICOM file(s).</value>
    public DicomIdentifier Identifier { get; set; }

    /// <summary>
    /// Gets or sets the error message detailing why the export failed.
    /// </summary>
    /// <value>The error message.</value>
    public string Error { get; set; }
}
