// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.Health.Dicom.Core.Features.Telemetry;

namespace Microsoft.Health.Dicom.Benchmark;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "This class is instantiated.")]
internal sealed class BenchmarkTelemetryClient : IDicomTelemetryClient
{
    private readonly TelemetryClient _telemetryClient;

    public BenchmarkTelemetryClient(TelemetryClient telemetryClient)
        => _telemetryClient = EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));

    public void TrackMetric(string name, int value)
        => _telemetryClient.GetMetric(name).TrackValue(value);

    public void TrackMetric(string name, long value)
        => _telemetryClient.GetMetric(name).TrackValue(value);
}
