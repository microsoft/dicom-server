// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.BlobMigration;

/// <summary>
/// Represents the options for a blob migration function.
/// </summary>
public class BlobMigrationOptions
{
    internal const string SectionName = "BlobMigration";

    /// <summary>
    /// Gets or sets the number of threads available for each batch.
    /// </summary>
    [Range(-1, int.MaxValue)]
    public int MaxParallelThreads { get; set; } = -1;

    /// <summary>
    /// Gets or sets the <see cref="ActivityRetryOptions"/> for copy activities.
    /// </summary>
    public ActivityRetryOptions RetryOptions { get; set; }
}
