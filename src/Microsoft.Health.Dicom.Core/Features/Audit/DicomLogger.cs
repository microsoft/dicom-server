// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Audit;
using Microsoft.Health.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Audit;

/// <summary>
/// Provides mechanism to log an audit or diagnostic event using default logger.
/// </summary>
public class DicomLogger : IDicomLogger
{
    private const string AuditEventType = "AuditEvent";

    private const string Prefix = "dicomAdditionalInformation_";
    private const string StudyInstanceUID = $"{Prefix}studyInstanceUID";
    private const string SeriesInstanceUID = $"{Prefix}seriesInstanceUID";
    private const string SOPInstanceUID = $"{Prefix}sopInstanceUID";

    private static readonly string AuditMessageFormat =
        "ActionType: {ActionType}" + Environment.NewLine +
        "EventType: {EventType}" + Environment.NewLine +
        "Audience: {Audience}" + Environment.NewLine +
        "Authority: {Authority}" + Environment.NewLine +
        "RequestUri: {ResourceUri}" + Environment.NewLine +
        "Action: {Action}" + Environment.NewLine +
        "StatusCode: {StatusCode}" + Environment.NewLine +
        "CorrelationId: {CorrelationId}" + Environment.NewLine +
        "Claims: {Claims}";

    private static readonly string DiagnosticMessageFormat =
        "Message: {Message}" + Environment.NewLine +
        "Operation: {Operation}" + Environment.NewLine +
        "CorrelationId: {CorrelationId}" + Environment.NewLine +
        "Properties: {Properties}";

    private readonly SecurityConfiguration _securityConfiguration;
    private readonly ILogger<IDicomLogger> _logger;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;

    public DicomLogger(
        IOptions<SecurityConfiguration> securityConfiguration,
        ILogger<IDicomLogger> logger,
        IDicomRequestContextAccessor dicomRequestContextAccessor)
    {
        EnsureArg.IsNotNull(securityConfiguration?.Value, nameof(securityConfiguration));
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));

        _securityConfiguration = securityConfiguration.Value;
        _logger = logger;
        _dicomRequestContextAccessor = dicomRequestContextAccessor;
    }

    /// <inheritdoc />
    public void LogAudit(
        AuditAction auditAction,
        string operation,
        Uri requestUri,
        HttpStatusCode? statusCode,
        string correlationId,
        string callerIpAddress,
        IReadOnlyCollection<KeyValuePair<string, string>> callerClaims,
        IReadOnlyDictionary<string, string> customHeaders = null)
    {
        string claimsInString = null;
        string customerHeadersInString = null;

        if (callerClaims != null)
        {
            claimsInString = string.Join(";", callerClaims.Select(claim => $"{claim.Key}={claim.Value}"));
        }

        if (customHeaders != null)
        {
            customerHeadersInString = string.Join(";", customHeaders.Select(header => $"{header.Key}={header.Value}"));
        }

#pragma warning disable CA2254
        // AuditMessageFormat is not const and erroneously flags CA2254.
        // While the template does indeed change per OS, it does not change the variables in use.
        _logger.LogInformation(
            AuditMessageFormat,
            auditAction,
            AuditEventType,
            _securityConfiguration.Authentication?.Audience,
            _securityConfiguration.Authentication?.Authority,
            requestUri,
            operation,
            statusCode,
            correlationId,
            claimsInString,
            customerHeadersInString);
#pragma warning restore CA2254
    }

    public void LogDiagnostic(
        string message,
        InstanceIdentifier instanceIdentifier)
    {
        EnsureArg.IsNotNull(message, nameof(message));
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));

        IRequestContext dicomRequestContext = _dicomRequestContextAccessor.RequestContext;

        var props = new string[]
        {
            $"{StudyInstanceUID}={instanceIdentifier.StudyInstanceUid}",
            $"{SeriesInstanceUID}={instanceIdentifier.SeriesInstanceUid}",
            $"{SOPInstanceUID}={instanceIdentifier.SopInstanceUid}"
        };

        var parsedProperties = string.Join(";", props);
        var operation = $"{dicomRequestContext.RouteName} / {dicomRequestContext.Method}";

#pragma warning disable CA2254
        // AuditMessageFormat is not const and erroneously flags CA2254.
        // While the template does indeed change per OS, it does not change the variables in use.
        _logger.LogInformation(
            DiagnosticMessageFormat,
            message,
            operation,
            dicomRequestContext.CorrelationId,
            parsedProperties);
#pragma warning restore CA2254
    }
}
