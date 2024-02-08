// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;

namespace Microsoft.Health.Dicom.Api.Logging;

/// <summary>
/// Class extends AppInsights telemtry to include custom properties
/// </summary>
internal class TelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly bool _enableDataPartitions;
    private readonly bool _enableExport;
    private readonly bool _enableExternalStore;
    private const string ApiVersionColumnName = "ApiVersion";
    private const string EnableDataPartitions = "EnableDataPartitions";
    private const string EnableExport = "EnableExport";
    private const string EnableExternalStore = "EnableExternalStore";
    private const string UserAgent = "UserAgent";

    public TelemetryInitializer(IHttpContextAccessor httpContextAccessor, IOptions<FeatureConfiguration> featureConfiguration)
    {
        _httpContextAccessor = EnsureArg.IsNotNull(httpContextAccessor, nameof(httpContextAccessor));
        EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
        _enableDataPartitions = featureConfiguration.Value.EnableDataPartitions;
        _enableExport = featureConfiguration.Value.EnableExport;
        _enableExternalStore = featureConfiguration.Value.EnableExternalStore;
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
        var feature = _httpContextAccessor.HttpContext?.Features.Get<IApiVersioningFeature>();

        if (feature?.RouteParameter != null)
        {
            version = feature.RawRequestedApiVersion;
        }

        if (version == null)
        {
            return;
        }

        requestTelemetry.Properties[ApiVersionColumnName] = version;
        requestTelemetry.Properties[EnableDataPartitions] = _enableDataPartitions.ToString();
        requestTelemetry.Properties[EnableExport] = _enableExport.ToString();
        requestTelemetry.Properties[EnableExternalStore] = _enableExternalStore.ToString();
        requestTelemetry.Properties[UserAgent] = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent;
    }
}
