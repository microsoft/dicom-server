// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Models.Operations;

/// <summary>
/// Represents options for activity batching frequently used by "fan-out/fan-in" scenarios.
/// </summary>
public sealed class BatchingOptions
{
    /// <summary>
    /// Gets or sets the size of each batch.
    /// </summary>
    /// <remarks>
    /// A batch typically represents the input to an activity.
    /// </remarks>
    /// <value>The number of elements in each batch.</value>
    [Range(1, int.MaxValue)]
    public int Size { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent batches processed at a given time.
    /// </summary>
    /// <remarks>
    /// This typically represents the maximum number of concurrent activities.
    /// </remarks>
    /// <value>The maximum number of batches that may be processed at once.</value>
    [Range(1, int.MaxValue)]
    public int MaxParallelCount { get; set; }

    /// <summary>
    /// Gets the maximum number of elements that are processed concurrently.
    /// </summary>
    /// <value>
    /// The value of the <see cref="Size"/> property multiplied by the
    /// value of the <see cref="MaxParallelCount"/> property.
    /// </value>
    [JsonIgnore]
    public int MaxParallelElements => Size * MaxParallelCount;
}
