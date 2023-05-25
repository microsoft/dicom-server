// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.Metrics;

namespace Microsoft.Health.Dicom.Core.Features.Telemetry;

public sealed class UpdateMeter : IDisposable
{
    private readonly Meter _meter;

    public UpdateMeter()
    {
        _meter = new Meter($"{OpenTelemetryLabels.BaseMeterName}.Update", "1.0");
        UpdatedInstances = _meter.CreateCounter<int>(nameof(UpdatedInstances), description: "Count of instances updated successfully");
    }

    public Counter<int> UpdatedInstances { get; }

    public void Dispose()
        => _meter.Dispose();
}
