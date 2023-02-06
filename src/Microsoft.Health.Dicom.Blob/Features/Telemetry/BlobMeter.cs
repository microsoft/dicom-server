// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using EnsureThat;

namespace Microsoft.Health.Dicom.Blob.Features.Telemetry;
public sealed class BlobMeter : IDisposable
{
    private readonly Meter _meter;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposal is done through Dispose method.")]
    public BlobMeter()
        : this(new Meter("Microsoft.Health.Dicom.Blob.Features.Storage", "1.0"))
    {
    }

    internal BlobMeter(Meter meter)
    {
        _meter = EnsureArg.IsNotNull(meter, nameof(meter));
        JsonSerializationException = _meter.CreateCounter<double>(nameof(JsonSerializationException));
        JsonDeserializationException = _meter.CreateCounter<double>(nameof(JsonDeserializationException));
    }

    public Counter<double> JsonSerializationException { get; }
    public Counter<double> JsonDeserializationException { get; }


    public void Dispose()
        => _meter.Dispose();
}
