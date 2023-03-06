// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.Metrics;
using Microsoft.Health.Dicom.Core.Features.Telemetry;

namespace Microsoft.Health.Dicom.Blob.Features.Telemetry;

public sealed class BlobStoreMeter : IDisposable
{
    private readonly Meter _meter;

    public BlobStoreMeter()
    {
        _meter = new Meter($"{OpenTelemetryLabels.BaseMeterName}.Store.Blob", "1.0");
        JsonSerializationException = _meter.CreateCounter<int>(nameof(JsonSerializationException), description: "Json serialization exception");
    }

    public Counter<int> JsonSerializationException { get; }

    public void Dispose()
        => _meter.Dispose();
}
