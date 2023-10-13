// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
    private const int MaxShoeboxPropertySize = 32 * 1024;
    private const string ForwardLogFlag = "forwardLog";
    private const string Prefix = "dicomAdditionalInformation_";
    private const string StudyInstanceUID = $"{Prefix}studyInstanceUID";
    private const string SeriesInstanceUID = $"{Prefix}seriesInstanceUID";
    private const string SOPInstanceUID = $"{Prefix}sopInstanceUID";
    private const string PartitionName = $"{Prefix}partitionName";
    private const string InputPayload = $"{Prefix}input";
    private const string OperationId = $"{Prefix}operationId";

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
        telemetry.Properties.Add(PartitionName, instanceIdentifier.Partition.Name);

        telemetryClient.TrackTrace(telemetry);
    }

    /// <summary>
    /// Emits a trace log with forwarding flag set.
    /// </summary>
    /// <remarks>NOTE - do not use this if reporting on any specific instance. Only use as high level remarks. Attempt to use identifiers wherever possible</remarks>
    /// <param name="telemetryClient">client to use to emit the trace</param>
    /// <param name="message">message to set on the trace log</param>
    public static void ForwardLogTrace(
        this TelemetryClient telemetryClient,
        string message)
    {
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        EnsureArg.IsNotNull(message, nameof(message));

        var telemetry = new TraceTelemetry(message);
        telemetry.Properties.Add(ForwardLogFlag, bool.TrueString);

        telemetryClient.TrackTrace(telemetry);
    }

    /// <summary>
    /// Emits a trace log with forwarding flag set for operations and adds the required properties to telemetry.
    /// </summary>
    /// <param name="telemetryClient">client to use to emit the trace</param>
    /// <param name="message">message to set on the trace log</param>
    /// <param name="operationId">operation id</param>
    /// <param name="input">Input payload to pass to the forward logger</param>
    public static void ForwardOperationLogTrace(
        this TelemetryClient telemetryClient,
        string message,
        string operationId,
        string input)
    {
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        EnsureArg.IsNotNull(message, nameof(message));

        // Shoebox property size has a limitation of 32 KB which is why the diagnostic log is split into multiple messages
        int startIndex = 0, offset = 0, inputSize = input.Length;
        while (startIndex < inputSize)
        {
            offset = Math.Min(MaxShoeboxPropertySize, input.Length - startIndex);
            ForwardOperationLogTraceWithSizeLimit(telemetryClient, message, operationId, input.Substring(startIndex, offset));
            startIndex += offset;
        }
    }

    private static void ForwardOperationLogTraceWithSizeLimit(
        TelemetryClient telemetryClient,
        string message,
        string operationId,
        string input)
    {
        var telemetry = new TraceTelemetry(message);
        telemetry.Properties.Add(InputPayload, input);
        telemetry.Properties.Add(OperationId, operationId);
        telemetry.Properties.Add(ForwardLogFlag, bool.TrueString);

        telemetryClient.TrackTrace(telemetry);
    }
}
