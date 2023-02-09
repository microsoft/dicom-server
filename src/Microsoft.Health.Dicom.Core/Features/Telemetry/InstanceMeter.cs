// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.Metrics;

namespace Microsoft.Health.Dicom.Core.Features.Telemetry;

public sealed class InstanceMeter : IDisposable
{
    private readonly Meter _meter;

    public InstanceMeter()
    {
        _meter = new Meter($"{OpenTelemetryLabels.BaseMeterName}.Instance", "1.0");
        InstanceCount = _meter.CreateCounter<long>(nameof(InstanceCount), description: "Total number of instances processed");
        RetrieveInstanceCount = _meter.CreateCounter<long>(nameof(InstanceCount), description: "Total number of instances retrieved");
        OldestRequestedDeletion = _meter.CreateCounter<long>(nameof(OldestRequestedDeletion), "seconds", "Oldest instance waiting to be deleted in seconds");
        CountDeletionsMaxRetry = _meter.CreateCounter<long>(nameof(CountDeletionsMaxRetry), description: "Number of exhausted instance deletion attempts");
        TotalInstanceBytes = _meter.CreateHistogram<double>(nameof(TotalInstanceBytes), "bytes", "Total length of the instance");
        MinInstanceBytes = _meter.CreateHistogram<double>(nameof(MinInstanceBytes), "bytes", "Minimum length of the instance");
        MaxInstanceBytes = _meter.CreateHistogram<double>(nameof(MaxInstanceBytes), "bytes", "Maximum length of the instance");
    }

    public Counter<long> InstanceCount { get; }

    public Counter<long> RetrieveInstanceCount { get; }

    public Counter<long> OldestRequestedDeletion { get; }

    public Counter<long> CountDeletionsMaxRetry { get; }

    public Histogram<double> TotalInstanceBytes { get; }

    public Histogram<double> MinInstanceBytes { get; }

    public Histogram<double> MaxInstanceBytes { get; }

    public void Dispose()
        => _meter.Dispose();
}
