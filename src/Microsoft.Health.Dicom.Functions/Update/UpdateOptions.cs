// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Update;

/// <summary>
/// Represents the options for a "update" function.
/// </summary>
public class UpdateOptions
{
    internal const string SectionName = "Update";

    /// <summary>
    /// Gets or sets the number of DICOM instances updated in a single batch inside a activity.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the number of threads available for each batch.
    /// </summary>
    [Range(-1, int.MaxValue)]
    public int MaxParallelThreads { get; set; } = -1;

    /// <summary>
    /// Gets or sets the <see cref="ActivityRetryOptions"/> for update activities.
    /// </summary>
    public ActivityRetryOptions RetryOptions { get; set; }
}
