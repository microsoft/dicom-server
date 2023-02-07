// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.Metrics;
using Microsoft.Health.Dicom.Core.Features.Telemetry;

namespace Microsoft.Health.Dicom.Blob.Features.Telemetry;
public sealed class BlobMeter : IDisposable
{
    private readonly Meter _meter;

    public BlobMeter()
    {
        _meter = new Meter($"{OpenTelemetryLabels.BaseMeterName}.Blob", "1.0");
        JsonSerializationException = _meter.CreateCounter<double>(nameof(JsonSerializationException), "count", "Json serialization exception");
        JsonDeserializationException = _meter.CreateCounter<double>(nameof(JsonDeserializationException), "count", "Json deserialization exception");
    }

    public Counter<double> JsonSerializationException { get; }

    public Counter<double> JsonDeserializationException { get; }

    public void Dispose()
        => _meter.Dispose();
}
