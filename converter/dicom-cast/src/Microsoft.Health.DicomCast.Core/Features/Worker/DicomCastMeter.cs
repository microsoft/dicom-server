// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.Metrics;

namespace Microsoft.Health.DicomCast.Core.Features.Worker;
public sealed class DicomCastMeter : IDisposable
{
    private readonly Meter _meter;

    public DicomCastMeter()
    {
        _meter = new Meter("Microsoft.Health.DicomCast", "1.0");
        CastToFhirForbidden = _meter.CreateCounter<double>(nameof(CastToFhirForbidden), "count", "Casting to Fhir forbidden");
        DicomToCastForbidden = _meter.CreateCounter<double>(nameof(DicomToCastForbidden), "count", "Dicom casting forbidden");
        CastMIUnavailable = _meter.CreateCounter<double>(nameof(CastMIUnavailable), "count", "Managed Identity unavailable");
        CastingFailedForOtherReasons = _meter.CreateCounter<double>(nameof(CastingFailedForOtherReasons), "count", "Casting failed due to other reasons");
    }

    public Counter<double> CastToFhirForbidden { get; }

    public Counter<double> DicomToCastForbidden { get; }

    public Counter<double> CastMIUnavailable { get; }

    public Counter<double> CastingFailedForOtherReasons { get; }

    public void Dispose()
        => _meter.Dispose();
}
