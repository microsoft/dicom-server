// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.Metrics;

namespace Microsoft.Health.Dicom.Core.Features.Telemetry;

public sealed class RetrieveMeter : IDisposable
{
    private readonly Meter _meter;

    public RetrieveMeter()
    {
        _meter = new Meter($"{OpenTelemetryLabels.BaseMeterName}.Retrieve", "1.0");
        RetrieveInstanceCount = _meter.CreateCounter<long>(nameof(RetrieveInstanceCount), description: "Total number of instances retrieved");
    }

    public Counter<long> RetrieveInstanceCount { get; }

    public void Dispose()
        => _meter.Dispose();
}
