// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.Metrics;

namespace Microsoft.Health.Dicom.Core.Features.Telemetry;

public sealed class DeleteMeter : IDisposable
{
    private readonly Meter _meter;

    public DeleteMeter()
    {
        _meter = new Meter($"{OpenTelemetryLabels.BaseMeterName}.Delete", "1.0");
        OldestRequestedDeletion = _meter.CreateCounter<long>(nameof(OldestRequestedDeletion), "seconds", "Oldest instance waiting to be deleted in seconds");
        CountDeletionsMaxRetry = _meter.CreateCounter<long>(nameof(CountDeletionsMaxRetry), description: "Number of exhausted instance deletion attempts");
    }

    public Counter<long> OldestRequestedDeletion { get; }

    public Counter<long> CountDeletionsMaxRetry { get; }

    public void Dispose()
        => _meter.Dispose();
}
