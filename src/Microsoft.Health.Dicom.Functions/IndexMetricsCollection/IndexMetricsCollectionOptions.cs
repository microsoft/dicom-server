// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Functions.IndexMetricsCollection;

/// <summary>
/// 
/// </summary>
public static class IndexMetricsCollectionOptions
{
    /// <summary>
    /// The default section name for <see cref="IndexMetricsCollectionOptions"/> in a configuration.
    /// </summary>
    public const string SectionName = "IndexMetricsCollection";

    /// <summary>
    /// Gets or sets the cron expression that indicates how frequently to run the index metrics collection function.
    /// </summary>
    /// <value>A value cron expression</value>
    [Required]
    public const string Frequency = "* * * * *"; // Every day at midnight
    // public const string Frequency = "0 0 * * *"; // Every day at midnight
}
