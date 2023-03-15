// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
        string version = null;
        try
        {
            version = _httpContextAccessor.HttpContext?.GetRequestedApiVersion()?.ToString();
        }
        catch (ArgumentNullException)
        {
            /*
             * GetRequestedApiVersion() is throwing argument null on urls like health/check which as no version.
             * logged a bug  https://github.com/dotnet/aspnet-api-versioning/issues/976
             */
        }
        if (version == null)
        {
            return;
        }

        requestTelemetry.Properties[ApiVersionColumnName] = version;
    }
}
