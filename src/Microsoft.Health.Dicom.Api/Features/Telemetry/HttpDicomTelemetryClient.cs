// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Dicom.Core.Features.Telemetry;

namespace Microsoft.Health.Dicom.Api.Features.Telemetry;

internal class HttpDicomTelemetryClient : IDicomTelemetryClient
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpDicomTelemetryClient(TelemetryClient telemetryClient, IHttpContextAccessor httpContextAccessor)
    {
        _telemetryClient = EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        _httpContextAccessor = EnsureArg.IsNotNull(httpContextAccessor, nameof(httpContextAccessor));
    }

    public void TrackMetric(string name, int value)
    {
        // Note: Context Items are prefixed so that the telemetry initializer knows which items to include in the telemetry
        _httpContextAccessor.HttpContext.Items[DicomTelemetry.ContextItemPrefix + name] = value;
        _telemetryClient.GetMetric(name).TrackValue(value);
    }

    public void TrackMetric(string name, long value)
    {
        // Note: Context Items are prefixed so that the telemetry initializer knows which items to include in the telemetry
        _httpContextAccessor.HttpContext.Items[DicomTelemetry.ContextItemPrefix + name] = value;
        _telemetryClient.GetMetric(name).TrackValue(value);
    }
}
