// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Export;

/// <summary>
/// Represents configurable settings that control the execution of export operations.
/// </summary>
public class ExportOptions
{
    internal const string SectionName = "Export";

    /// <summary>
    /// Gets or sets the number of threads available for each batch.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int BatchThreadCount { get; set; } = 5;

    /// <summary>
    /// Gets or sets the <see cref="ActivityRetryOptions"/> for export activities.
    /// </summary>
    [Required]
    public ActivityRetryOptions RetryOptions { get; set; }
}
