// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.BlobMigration.Models;

/// <summary>
/// Represents the arguments to the <see cref="CleanupDeletedDurableFunction.GetDeletedChangeFeedInstanceBatchesAsync"/> activity.
/// </summary>
public class CleanupDeletedBatchArguments
{
    /// <summary>
    /// Gets or sets the inclusive start and end watermark
    /// </summary>
    public WatermarkRange? BatchRange { get; set; }

    /// <summary>
    /// Gets or sets the number of DICOM instances processed by a single activity.
    /// </summary>
    public int BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the timestamp to filter change feed deleted instances
    /// </summary>
    public DateTime? FilterTimeStamp { get; set; }
}
