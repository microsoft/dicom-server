// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Functions.MetricsCollection;

/// <summary>
/// Options on collecting indexing metrics
/// </summary>
public class IndexMetricsCollectionOptions
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
    public string Frequency { get; set; }
}
