// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.Metrics;

namespace Microsoft.Health.Dicom.Core.Features.Telemetry;

public sealed class StoreMeter : IDisposable
{
    private readonly Meter _meter;

    public StoreMeter()
    {
        _meter = new Meter("Microsoft.Health.Dicom.Core.Features.Telemetry.Store", "1.0");
        IndexTagValidationError = _meter.CreateCounter<double>(nameof(IndexTagValidationError));
        DroppedInvalidTag = _meter.CreateCounter<double>(nameof(DroppedInvalidTag));
    }

    public Counter<double> IndexTagValidationError { get; }
    public Counter<double> DroppedInvalidTag { get; }


    public void Dispose()
        => _meter.Dispose();
}
