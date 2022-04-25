// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Functions.Export.Models;

/// <summary>
/// Represents the arguments to the <see cref="ExportDurableFunction.ExportBatchAsync"/> activity.
/// </summary>
public class ExportBatchArguments
{
    /// <summary>
    /// Gets or sets the source batch for the export operation.
    /// </summary>
    /// <value>The configuration describing the batch of data from the source.</value>
    public TypedConfiguration<ExportSourceType> Source { get; set; }

    /// <summary>
    /// Gets or sets the destination of the export operation.
    /// </summary>
    /// <value>The configuration describing the destination.</value>
    public TypedConfiguration<ExportDestinationType> Destination { get; set; }
}
