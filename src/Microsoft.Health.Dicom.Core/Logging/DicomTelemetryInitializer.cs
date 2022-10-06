// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Dicom.Core.Logging;

public class DicomTelemetryInitializer : ITelemetryInitializer
{
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;

    public DicomTelemetryInitializer(IDicomRequestContextAccessor dicomRequestContextAccessor)
    {
        _dicomRequestContextAccessor = dicomRequestContextAccessor;
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
#pragma warning disable CA1305
                _dicomRequestContextAccessor.RequestContext.PartCount.ToString());
#pragma warning restore CA1305
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
            if (!string.Equals(existingValue, value, StringComparison.OrdinalIgnoreCase))
            {
                telemetry.Properties[key] = value;
                // ReSharper disable once LocalizableElement
                Console.WriteLine($"The telemetry already contains the property of {key} with value {existingValue}. The new value is: {value}");
            }
        }
    }
}
