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
        _httpContextAccessor.HttpContext.Items[name] = value;
        _telemetryClient.GetMetric(name).TrackValue(value);
    }

    public void TrackMetric(string name, double value)
    {
        _httpContextAccessor.HttpContext.Items[name] = value;
        _telemetryClient.GetMetric(name).TrackValue(value);
    }
}
