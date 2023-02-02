// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using EnsureThat;

namespace Microsoft.Health.DicomCast.Core.Features.Worker;
public sealed class DicomCastMeter : IDisposable
{
    private readonly Meter _meter;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposal is done through Dipose method.")]
    public DicomCastMeter()
        : this(new Meter("Microsoft.Health.Cloud.DicomCast.Core.Features.Worker", "1.0"))
    {
    }

    internal DicomCastMeter(Meter meter)
    {
        _meter = EnsureArg.IsNotNull(meter, nameof(meter));
        CastToFhirForbidden = _meter.CreateCounter<double>(nameof(CastToFhirForbidden));
        DicomToCastForbidden = _meter.CreateCounter<double>(nameof(DicomToCastForbidden));
        CastMIUnavailable = _meter.CreateCounter<double>(nameof(CastMIUnavailable));
        CastingFailedForOtherReasons = _meter.CreateCounter<double>(nameof(CastingFailedForOtherReasons));
    }

    public Counter<double> CastToFhirForbidden { get; }
    public Counter<double> DicomToCastForbidden { get; }
    public Counter<double> CastMIUnavailable { get; }
    public Counter<double> CastingFailedForOtherReasons { get; }

    public void Dispose()
        => _meter.Dispose();
}
