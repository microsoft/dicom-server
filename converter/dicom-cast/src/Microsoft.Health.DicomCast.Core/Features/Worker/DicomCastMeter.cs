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
        CastToFhirForbidden = _meter.CreateCounter<int>(nameof(CastToFhirForbidden), description: "DicomCast failed due to a 403 (Forbidden) response from the FHIR server.");
        DicomToCastForbidden = _meter.CreateCounter<int>(nameof(DicomToCastForbidden), description: "Dicom casting forbidden");
        CastMIUnavailable = _meter.CreateCounter<int>(nameof(CastMIUnavailable), description: "Managed Identity unavailable");
        CastingFailedForOtherReasons = _meter.CreateCounter<int>(nameof(CastingFailedForOtherReasons), description: "Casting failed due to other reasons");
    }

    public Counter<int> CastToFhirForbidden { get; }

    public Counter<int> DicomToCastForbidden { get; }

    public Counter<int> CastMIUnavailable { get; }

    public Counter<int> CastingFailedForOtherReasons { get; }

    public void Dispose()
        => _meter.Dispose();
}
