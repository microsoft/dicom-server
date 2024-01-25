// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Health.Dicom.Core.Features.Telemetry;

namespace Microsoft.Health.Dicom.Functions.IndexMetricsCollection.Telemetry;

public sealed class IndexMetricsCollectionMeter : IDisposable
{
    private readonly Meter _meter;
    internal const string MeterName = "IndexMetricsCollection";

    public IndexMetricsCollectionMeter()
    {
        _meter = new Meter($"{OpenTelemetryLabels.BaseMeterName}.{MeterName}", "1.0");

        IndexMetricsCollectionsCompletedCounter =
            _meter.CreateCounter<long>(
                nameof(IndexMetricsCollectionsCompletedCounter),
                description: "Represents a successful run of the index metrics collection function.");
    }


    public static KeyValuePair<string, object>[] CreateTelemetryDimension(bool externalStoreEnabled, bool dataPartitionsEnabled) =>
        new[]
        {
            new KeyValuePair<string, object>("ExternalStoreEnabled", externalStoreEnabled),
            new KeyValuePair<string, object>("DataPartitionsEnabled", dataPartitionsEnabled),
        };

    /// <summary>
    /// Represents a successful run of the index metrics collection function
    /// </summary>
    public Counter<long> IndexMetricsCollectionsCompletedCounter { get; }

    public void Dispose()
        => _meter.Dispose();
}