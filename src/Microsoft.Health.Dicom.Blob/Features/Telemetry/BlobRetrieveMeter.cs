// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.Metrics;
using Microsoft.Health.Dicom.Core.Features.Telemetry;

namespace Microsoft.Health.Dicom.Blob.Features.Telemetry;

public sealed class BlobRetrieveMeter : IDisposable
{
    private readonly Meter _meter;

    public BlobRetrieveMeter()
    {
        _meter = new Meter($"{OpenTelemetryLabels.BaseMeterName}.Retrieve.Blob", "1.0");
        JsonDeserializationException = _meter.CreateCounter<int>(nameof(JsonDeserializationException), description: "Json deserialization exception");
    }

    public Counter<int> JsonDeserializationException { get; }

    public void Dispose()
        => _meter.Dispose();
}
