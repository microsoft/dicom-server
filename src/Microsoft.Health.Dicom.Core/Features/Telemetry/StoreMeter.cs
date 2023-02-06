// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Telemetry;
public sealed class StoreMeter : IDisposable
{
    private readonly Meter _meter;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposal is done through Dispose method.")]
    public StoreMeter()
        : this(new Meter("Microsoft.Health.Dicom.Core.Features.Telemetry.Store", "1.0"))
    {
    }

    internal StoreMeter(Meter meter)
    {
        _meter = EnsureArg.IsNotNull(meter, nameof(meter));
        IndexTagValidationError = _meter.CreateCounter<double>(nameof(IndexTagValidationError));
        DroppedInvalidTag = _meter.CreateCounter<double>(nameof(DroppedInvalidTag));
    }

    public Counter<double> IndexTagValidationError { get; }
    public Counter<double> DroppedInvalidTag { get; }


    public void Dispose()
        => _meter.Dispose();
}
