// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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

    private const string DicomPrefix = "Dicom_";

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

        IEnumerable<(string Key, string Value)> properties = _httpContextAccessor.HttpContext
            .Items
            .Select(x => (Key: x.Key.ToString(), x.Value))
            .Where(x => x.Key.StartsWith(DicomPrefix, StringComparison.Ordinal))
            .Select(x => (x.Key[DicomPrefix.Length..], x.Value?.ToString()));

        foreach ((string key, string value) in properties)
        {
            if (requestTelemetry.Properties.ContainsKey(key))
                requestTelemetry.Properties["DuplicateDimension"] = bool.TrueString;

            requestTelemetry.Properties[key] = value;
        }
    }
}