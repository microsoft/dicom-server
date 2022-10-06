// --------------------------------------------------------------------------
// <copyright file="DicomTelemetryInitializer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Cloud.Dicom.Logging.Initializers;

public class DicomTelemetryInitializer : ITelemetryInitializer
{
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;

    public DicomTelemetryInitializer(IDicomRequestContextAccessor dicomRequestContextAccessor)
    {
        this._dicomRequestContextAccessor = dicomRequestContextAccessor;
    }

    public void Initialize(ITelemetry telemetry)
    {
        var requestTelemetry = telemetry as RequestTelemetry;
        if (requestTelemetry == null)
        {
            return;
        }

        if (_dicomRequestContextAccessor.RequestContext != null)
        {
            AddProperty(
                requestTelemetry,
                "InstanceCount",
                _dicomRequestContextAccessor.RequestContext.PartCount.ToString());
            AddProperty(
                requestTelemetry,
                "RouteNameTestValue",
                _dicomRequestContextAccessor.RequestContext.RouteName);
        }
        else
        {
            AddProperty(
                requestTelemetry,
                "RequestContextIsNull",
                true.ToString());
        }
    }

    private static void AddProperty(ISupportProperties telemetry, string key, string value)
    {
        if (!telemetry.Properties.ContainsKey(key))
        {
            telemetry.Properties[key] = value;
        }
        else
        {
            string existingValue = telemetry.Properties[key];
            if (!string.Equals(existingValue, value, System.StringComparison.OrdinalIgnoreCase))
            {
                telemetry.Properties[key] = value;
                System.Console.WriteLine($"The telemetry already contains the property of {key} with value {existingValue}. The new value is: {value}");
            }
        }
    }
}