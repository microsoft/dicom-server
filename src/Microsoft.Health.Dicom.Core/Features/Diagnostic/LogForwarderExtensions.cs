// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Diagnostic;

/// <summary>
/// A log forwarder which sets a property designating a log to be forwarded.
/// </summary>
internal static class LogForwarderExtensions
{
    private const string ForwardLogFlag = "forwardLog";
    private const string Prefix = "dicomAdditionalInformation_";
    private const string StudyInstanceUID = $"{Prefix}studyInstanceUID";
    private const string SeriesInstanceUID = $"{Prefix}seriesInstanceUID";
    private const string SOPInstanceUID = $"{Prefix}sopInstanceUID";

    /// <summary>
    /// Emits a trace log with forwarding flag set and adds properties from instanceIdentifier as properties to telemetry.
    /// </summary>
    /// <param name="telemetryClient">client to use to emit the trace</param>
    /// <param name="message">message to set on the trace log</param>
    /// <param name="instanceIdentifier">identifier to use to set UIDs on log and telemetry properties</param>
    public static void ForwardLogTrace(
        this TelemetryClient telemetryClient,
        string message,
        InstanceIdentifier instanceIdentifier)
    {
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        EnsureArg.IsNotNull(message, nameof(message));
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));

        var telemetry = new TraceTelemetry(message);
        telemetry.Properties.Add(StudyInstanceUID, instanceIdentifier.StudyInstanceUid);
        telemetry.Properties.Add(SeriesInstanceUID, instanceIdentifier.SeriesInstanceUid);
        telemetry.Properties.Add(SOPInstanceUID, instanceIdentifier.SopInstanceUid);
        telemetry.Properties.Add(ForwardLogFlag, bool.TrueString);

        telemetryClient.TrackTrace(telemetry);
    }
}
