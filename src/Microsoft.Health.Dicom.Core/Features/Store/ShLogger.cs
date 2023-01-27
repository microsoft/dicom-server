// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.Health.Dicom.Core.Features.Store;

public static class ShLogger
{
    // private static readonly string Namespace = MetricIdentifier.DefaultMetricNamespace;
    private const string HelpLinkAttribute = "helpLink";
    private const string MessageAttribute = "message";
    private const string LogForwardingAttribute = "logToCustomerDicom";

    public static void LogTrace(TelemetryClient telemetryClient, Exception ex)
    {
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        EnsureArg.IsNotNull(ex, nameof(ex));

        var telemetry = new TraceTelemetry("Validation warning.");

        telemetry.Properties.Add("message", ex.Message ?? string.Empty);
        telemetry.Properties.Add("helpLink", ex.HelpLink ?? string.Empty);
        telemetry.Properties.Add(MessageAttribute, ex.Message ?? string.Empty);
        telemetry.Properties.Add(HelpLinkAttribute, ex.HelpLink ?? string.Empty);
        telemetry.Properties.Add(LogForwardingAttribute, true.ToString());

        telemetryClient.TrackTrace(telemetry);
    }
}
