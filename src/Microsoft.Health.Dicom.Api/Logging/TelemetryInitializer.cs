// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Health.Dicom.Api.Logging;

/// <summary>
/// Class extends AppInsights telemtry to include custom properties
/// </summary>
internal class TelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string ApiVersionColumnName = "ApiVersion";

    public TelemetryInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = EnsureArg.IsNotNull(httpContextAccessor, nameof(httpContextAccessor));
    }

    public void Initialize(ITelemetry telemetry)
    {
        AddApiVersionColumn(telemetry);
    }

    private void AddApiVersionColumn(ITelemetry telemetry)
    {
        var requestTelemetry = telemetry as RequestTelemetry;
        if (requestTelemetry == null)
        {
            return;
        }

        string version = _httpContextAccessor.HttpContext?.GetRequestedApiVersion()?.ToString();
        if (version == null)
        {
            return;
        }

        requestTelemetry.Properties[ApiVersionColumnName] = version;
    }
}
