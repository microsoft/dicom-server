// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using EnsureThat;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Health.Dicom.Api.Logging;

public class DicomTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DicomTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = EnsureArg.IsNotNull(httpContextAccessor, nameof(httpContextAccessor));
    }

    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is RequestTelemetry requestTelemetry)
            AddPropertiesFromHttpContextItems(requestTelemetry);
    }

    private void AddPropertiesFromHttpContextItems(RequestTelemetry requestTelemetry)
    {
        if (_httpContextAccessor.HttpContext == null)
        {
            return;
        }

        foreach ((string key, string value) in _httpContextAccessor.HttpContext.Items.Select(x => (x.Key.ToString(), x.Value?.ToString())))
        {
            if (!requestTelemetry.Properties.ContainsKey(key))
            {
                requestTelemetry.Properties[key] = value;
            }
        }
    }
}
