// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Diagnostic;

/// <summary>
/// Used to log audit events or specific diagnostic events that need specialized sinks rather than the injected ILogger.
/// </summary>
public class DicomForwardingLogger : IDicomForwardingLogger
{
    private const string Prefix = "dicomAdditionalInformation_";
    private const string StudyInstanceUID = $"{Prefix}studyInstanceUID";
    private const string SeriesInstanceUID = $"{Prefix}seriesInstanceUID";
    private const string SOPInstanceUID = $"{Prefix}sopInstanceUID";

    private readonly ILogger<IDicomForwardingLogger> _logger;

    public DicomForwardingLogger(ILogger<IDicomForwardingLogger> logger)
    {
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    /// <summary>
    /// Logs an audit event.
    /// </summary>
    /// <param name="auditAction">What type of audit is being performed. Example: Executing</param>
    /// <param name="customHeaders">Custom headers to include with audit event</param>
    public void LogAudit(
        AuditAction auditAction,
        IReadOnlyDictionary<string, string> customHeaders = null)
    {
        string log = string.Join(
            ", ",
            auditAction.ToString("F"),
            BuildCustomHeaders(customHeaders));

        _logger.LogInformation("Audit Log: {{ {Log} }}", log);
    }

    /// <summary>
    /// Logs a diagnostic event.
    /// </summary>
    /// <param name="message">Trace message to log with event to help understand what is going on in context of app.</param>
    /// <param name="instanceIdentifier">Dicom instance identifier to include as part of log.</param>
    public void LogDiagnostic(
        string message,
        InstanceIdentifier instanceIdentifier)
    {
        EnsureArg.IsNotNull(message, nameof(message));
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));

        string log = string.Join(
            ", ",
            message,
            BuildParsedProperties(instanceIdentifier));

        _logger.LogInformation("Diagnostic Log: {{ {Log} }}", log);
    }


    private static string BuildCustomHeaders(IReadOnlyDictionary<string, string> customHeaders)
    {
        string message = string.Empty;
        if (customHeaders != null && customHeaders.Count > 0)
        {
            var headersString = string.Join(", ", customHeaders.Select(kvp => kvp.ToString()));
            message += $"Custom Headers: {{ {headersString} }}";
        }

        return message;
    }


    private static string BuildParsedProperties(InstanceIdentifier instanceIdentifier)
    {
        var props = new string[]
        {
            $"{StudyInstanceUID}={instanceIdentifier.StudyInstanceUid}",
            $"{SeriesInstanceUID}={instanceIdentifier.SeriesInstanceUid}",
            $"{SOPInstanceUID}={instanceIdentifier.SopInstanceUid}"
        };

        var parsedProperties = string.Join(";", props);
        return parsedProperties;
    }
}
