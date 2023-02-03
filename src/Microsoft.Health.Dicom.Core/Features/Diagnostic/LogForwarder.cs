// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FellowOakDicom;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.Health.Dicom.Core.Features.Diagnostic;

public static class LogForwarder
{
    private const string ForwardLogFlag = "forwardLog";
    private const string Prefix = "dicomAdditionalInformation_";
    private const string StudyInstanceUID = $"{Prefix}studyInstanceUID";
    private const string SeriesInstanceUID = $"{Prefix}seriesInstanceUID";
    private const string SOPInstanceUID = $"{Prefix}sopInstanceUID";

    public static void LogTrace(
        TelemetryClient telemetryClient,
        string message,
        DicomDataset dataset)
    {
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        EnsureArg.IsNotNull(message, nameof(message));
        EnsureArg.IsNotNull(dataset, nameof(dataset));

        var telemetry = new TraceTelemetry(message ?? string.Empty);
        telemetry.Properties.Add(StudyInstanceUID, dataset.GetString(DicomTag.StudyInstanceUID));
        telemetry.Properties.Add(SeriesInstanceUID, dataset.GetString(DicomTag.SeriesInstanceUID));
        telemetry.Properties.Add(SOPInstanceUID, dataset.GetString(DicomTag.SOPInstanceUID));
        telemetry.Properties.Add(ForwardLogFlag, true.ToString());

        telemetryClient.TrackTrace(telemetry);
    }
}
