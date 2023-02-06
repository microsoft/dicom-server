// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Telemetry;
public sealed class InstanceMeter : IDisposable
{
    private readonly Meter _meter;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposal is done through Dispose method.")]
    public InstanceMeter()
        : this(new Meter("Microsoft.Health.Dicom.Core.Features.Telemetry.Instance", "1.0"))
    {
    }

    internal InstanceMeter(Meter meter)
    {
        _meter = EnsureArg.IsNotNull(meter, nameof(meter));
        InstanceCount = _meter.CreateCounter<double>(nameof(InstanceCount));
        TotalInstanceBytes = _meter.CreateHistogram<double>(nameof(TotalInstanceBytes));
        MinInstanceBytes = _meter.CreateHistogram<double>(nameof(MinInstanceBytes));
        MaxInstanceBytes = _meter.CreateHistogram<double>(nameof(MaxInstanceBytes));
    }

    public Counter<double> InstanceCount { get; }
    public Histogram<double> TotalInstanceBytes { get; }
    public Histogram<double> MinInstanceBytes { get; }
    public Histogram<double> MaxInstanceBytes { get; }

    public void Dispose()
        => _meter.Dispose();
}
