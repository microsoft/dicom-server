// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Options;
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

    /// <summary>
    /// Emits a trace log with forwarding flag set and adds properties from instanceIdentifier as properties to telemetry.
    /// </summary>
    /// <param name="telemetryClient">client to use to emit the trace</param>
    /// <param name="message">message to set on the trace log</param>
    /// <param name="operationId">operation id</param>
    /// <param name="value">Object to pass to the forward logger</param>
    /// <param name="jsonSerializerOptions">Json serialization options</param>
    public static void ForwardLogTrace<T>(
        this TelemetryClient telemetryClient,
        string message,
        string operationId,
        T value,
        IOptions<JsonSerializerOptions> jsonSerializerOptions)
    {
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        EnsureArg.IsNotNull(message, nameof(message));
        EnsureArg.IsNotNull(jsonSerializerOptions?.Value, nameof(jsonSerializerOptions));

        string property = JsonSerializer.Serialize(value, jsonSerializerOptions.Value);

        var telemetry = new TraceTelemetry(message);
        telemetry.Properties.Add(nameof(T), property);
        telemetry.Properties.Add("operation_id", operationId);

        telemetryClient.TrackTrace(telemetry);
    }
}
