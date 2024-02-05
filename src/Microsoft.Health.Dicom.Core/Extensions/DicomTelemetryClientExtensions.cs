// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.ApplicationInsights;

namespace Microsoft.Health.Dicom.Core.Extensions;

internal static class DicomTelemetryClientExtensions
{
    public static void TrackInstanceCount(this TelemetryClient telemetryClient, int count)
    {
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        telemetryClient.TrackMetric("Dicom_InstanceCount", count);
    }

    public static void TrackTotalInstanceBytes(this TelemetryClient telemetryClient, long bytes)
    {
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        telemetryClient.TrackMetric("Dicom_TotalInstanceBytes", bytes);
    }

    public static void TrackMinInstanceBytes(this TelemetryClient telemetryClient, long bytes)
    {
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        telemetryClient.TrackMetric("Dicom_MinInstanceBytes", bytes);
    }

    public static void TrackMaxInstanceBytes(this TelemetryClient telemetryClient, long bytes)
    {
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        telemetryClient.TrackMetric("Dicom_MaxInstanceBytes", bytes);
    }
}