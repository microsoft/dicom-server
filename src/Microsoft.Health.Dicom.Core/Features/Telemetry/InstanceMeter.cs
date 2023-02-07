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
        _meter = new Meter("Microsoft.Health.Dicom.Core.Features.Telemetry.Instance", "1.0");
        InstanceCount = _meter.CreateCounter<double>(nameof(InstanceCount));
        OldestRequestedDeletion = _meter.CreateCounter<double>(nameof(OldestRequestedDeletion));
        CountDeletionsMaxRetry = _meter.CreateCounter<double>(nameof(CountDeletionsMaxRetry));
        TotalInstanceBytes = _meter.CreateHistogram<double>(nameof(TotalInstanceBytes));
        MinInstanceBytes = _meter.CreateHistogram<double>(nameof(MinInstanceBytes));
        MaxInstanceBytes = _meter.CreateHistogram<double>(nameof(MaxInstanceBytes));
    }

    public Counter<double> InstanceCount { get; }
    public Counter<double> OldestRequestedDeletion { get; }
    public Counter<double> CountDeletionsMaxRetry { get; }
    public Histogram<double> TotalInstanceBytes { get; }
    public Histogram<double> MinInstanceBytes { get; }
    public Histogram<double> MaxInstanceBytes { get; }

    public void Dispose()
        => _meter.Dispose();
}
