// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Health.Dicom.Core.Logging;

public class DicomTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DicomTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Initialize(ITelemetry telemetry)
    {
        RequestTelemetry requestTelemetry = telemetry as RequestTelemetry;
        AddPropertiesFromHttpContextItems(requestTelemetry);
    }

    private void AddPropertiesFromHttpContextItems(RequestTelemetry requestTelemetry)
    {
        if (requestTelemetry == null || _httpContextAccessor.HttpContext == null)
        {
            return;
        }

        Dictionary<string, string> items = _httpContextAccessor.HttpContext.Items.ToDictionary(
            k => k.Key.ToString(),
            k => k.Value.ToString());

        foreach (KeyValuePair<string, string> entry in items)
        {
            requestTelemetry.Properties[entry.Key] = entry.Value;
        }
    }
}
