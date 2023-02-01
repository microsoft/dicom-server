// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.Health.Dicom.Core.Features.Diagnostic;

public static class LogExporter
{
    private const string LogForwardingAttribute = "logToCustomerDicom";

    public static void LogTrace(TelemetryClient telemetryClient, string message)
    {
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        EnsureArg.IsNotNull(message, nameof(message));

        var telemetry = new TraceTelemetry(message ?? string.Empty);
        telemetry.Properties.Add(LogForwardingAttribute, true.ToString());

        telemetryClient.TrackTrace(telemetry);
    }
}
