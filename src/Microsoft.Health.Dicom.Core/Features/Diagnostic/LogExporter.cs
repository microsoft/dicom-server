// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FellowOakDicom;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.Health.Dicom.Core.Features.Diagnostic;

public static class LogExporter
{
    private const string LogForwardingAttribute = "logToCustomerDicom";
    private const string StudyInstanceUid = "studyInstanceUID";
    private const string SeriesInstanceUid = "seriesInstanceUID";
    private const string SOPInstanceUid = "sopInstanceUID";

    public static void LogTrace(
        TelemetryClient telemetryClient,
        string message,
        DicomDataset dataset)
    {
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        EnsureArg.IsNotNull(message, nameof(message));

        var telemetry = new TraceTelemetry(message ?? string.Empty);
        telemetry.Properties.Add(StudyInstanceUid, dataset.GetString(DicomTag.StudyInstanceUID));
        telemetry.Properties.Add(SeriesInstanceUid, dataset.GetString(DicomTag.SeriesInstanceUID));
        telemetry.Properties.Add(SOPInstanceUid, dataset.GetString(DicomTag.SOPInstanceUID));
        telemetry.Properties.Add(LogForwardingAttribute, true.ToString());

        telemetryClient.TrackTrace(telemetry);
    }
}
