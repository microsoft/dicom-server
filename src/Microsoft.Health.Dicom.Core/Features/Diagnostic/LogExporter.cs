// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.Health.Dicom.Core.Features.Diagnostic;

public static class LogExporter
{
    private const string MessageAttribute = "message";
    private const string LogForwardingAttribute = "logToCustomerDicom";

    public static void LogException(TelemetryClient telemetryClient, Exception ex, string message = null)
    {
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        EnsureArg.IsNotNull(ex, nameof(ex));

        var telemetry = new ExceptionTelemetry(ex);
        telemetry.Properties.Add(MessageAttribute, message ?? ex.Message ?? string.Empty);
        telemetry.Properties.Add(LogForwardingAttribute, true.ToString());

        telemetryClient.TrackException(telemetry);
    }
}
