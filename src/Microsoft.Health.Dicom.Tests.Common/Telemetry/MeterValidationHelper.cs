// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenTelemetry.Metrics;

namespace Microsoft.Health.Dicom.Tests.Common.Telemetry;

public class MeterValidationHelper
{
    public static Dictionary<string, object> GetTags(MetricPoint metricPoint)
    {
        var tags = new Dictionary<string, object>();
        foreach (var pair in metricPoint.Tags)
        {
            tags.Add(pair.Key, pair.Value);
        }

        return tags;
    }

    public static Collection<MetricPoint> GetMetricPoints(string metricName, List<Metric> _exportedItems)
    {
        var metricItems = _exportedItems.Where(item => item.Name.Equals(metricName, StringComparison.Ordinal)).ToList();
        MetricPointsAccessor accessor = metricItems.First().GetMetricPoints();
        var metrics = new Collection<MetricPoint>();
        foreach (MetricPoint metricPoint in accessor)
        {
            metrics.Add(metricPoint);
        }

        return metrics;
    }
}