// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Functions.Export;

/// <summary>
/// Represents input to the export operation.
/// </summary>
public class ExportInput
{
    /// <summary>
    /// Gets or sets the source of the export operation.
    /// </summary>
    /// <value>The options describing the source.</value>
    public ExportDataOptions<ExportSourceType> Source { get; set; }

    /// <summary>
    /// Gets or sets the destination of the export operation.
    /// </summary>
    /// <value>The options describing the destination.</value>
    public ExportDataOptions<ExportDestinationType> Destination { get; set; }

    /// <summary>
    /// Gets or sets the settings that dictate how the operation should be parallelized.
    /// </summary>
    /// <value>A set of settings related to batching DICOM files for export.</value>
    public BatchingOptions Batching { get; set; }

    /// <summary>
    /// Gets or sets the DICOM data partition from which the data is read.
    /// </summary>
    /// <value>A DICOM partition entry.</value>
    public Partition Partition { get; set; }

    /// <summary>
    /// Gets or sets the URI for containing the errors for this operation, if any.
    /// </summary>
    /// <value>The <see cref="Uri"/> for the resource containg export errors.</value>
    public Uri ErrorHref { get; set; }
}
