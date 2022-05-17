// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Core.Models.Export;

/// <summary>
/// Represents the desired specification for an export operation,
/// describing both the data to be exported and its destination.
/// </summary>
public class ExportSpecification
{
    /// <summary>
    /// Gets or sets the source of the export operation.
    /// </summary>
    /// <value>The options describing the source.</value>
    [Required]
    public ExportDataOptions<ExportSourceType> Source { get; set; }

    /// <summary>
    /// Gets or sets the destination of the export operation.
    /// </summary>
    /// <value>The options describing the destination.</value>
    [Required]
    public ExportDataOptions<ExportDestinationType> Destination { get; set; }
}
