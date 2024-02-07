// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OpenTelemetry.Metrics;

namespace Microsoft.Health.Dicom.Core.Features.Telemetry;

/// <summary>
/// Since only enumerators are exposed publicly for working with tags or getting the collection of
/// metrics, these extension facilitate getting both.
/// </summary>
public static class MetricPointExtensions
{
    /// <summary>
    /// Get tags key value pairs from metric point.
    /// </summary>
    public static Dictionary<string, object> GetTags(this MetricPoint metricPoint)
    {
        var tags = new Dictionary<string, object>();
        foreach (var pair in metricPoint.Tags)
        {
            tags.Add(pair.Key, pair.Value);
        }

        return tags;
    }

    /// <summary>
    /// Get all metrics emitted after flushing.
    /// </summary>
    [SuppressMessage("Performance", "CA1859: Use concrete types when possible for improved performance", Justification = "Result should be read-only.")]
    public static IReadOnlyList<MetricPoint> GetMetricPoints(this ICollection<Metric> exportedItems, string metricName)
    {
        MetricPointsAccessor accessor = exportedItems
            .Single(item => item.Name.Equals(metricName, StringComparison.Ordinal))
            .GetMetricPoints();
        var metrics = new List<MetricPoint>();
        foreach (MetricPoint metricPoint in accessor)
        {
            metrics.Add(metricPoint);
        }

        return metrics;
    }
}