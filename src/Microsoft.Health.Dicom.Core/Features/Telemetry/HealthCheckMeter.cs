// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Telemetry;
public sealed class HealthCheckMeter : IDisposable
{
    private readonly Meter _meter;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposal is done through Dispose method.")]
    public HealthCheckMeter()
        : this(new Meter("Microsoft.Health.Dicom.Core.Features.Telemetry.HealthCheck", "1.0"))
    {
    }

    internal HealthCheckMeter(Meter meter)
    {
        _meter = EnsureArg.IsNotNull(meter, nameof(meter));
        OldestRequestedDeletion = _meter.CreateCounter<double>(nameof(OldestRequestedDeletion));
        CountDeletionsMaxRetry = _meter.CreateCounter<double>(nameof(CountDeletionsMaxRetry));
    }

    public Counter<double> OldestRequestedDeletion { get; }
    public Counter<double> CountDeletionsMaxRetry { get; }

    public void Dispose()
        => _meter.Dispose();
}
