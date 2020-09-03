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
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Core.Configs;

namespace Microsoft.Health.Dicom.Api.Features.Audit
{
    /// <summary>
    /// Provides mechanism to log the audit event using default logger.
    /// </summary>
    public class AuditLogger : IAuditLogger
    {
        private const string AuditEventType = "AuditEvent";

        private static string auditMessageFormat =
            "ActionType: {ActionType}" + Environment.NewLine +
            "EventType: {EventType}" + Environment.NewLine +
            "Audience: {Audience}" + Environment.NewLine +
            "Authority: {Authority}" + Environment.NewLine +
            "RequestUri: {ResourceUri}" + Environment.NewLine +
            "Action: {Action}" + Environment.NewLine +
            "StatusCode: {StatusCode}" + Environment.NewLine +
            "CorrelationId: {CorrelationId}" + Environment.NewLine +
            "Claims: {Claims}";

        private readonly SecurityConfiguration _securityConfiguration;
        private readonly ILogger<IAuditLogger> _logger;

        public AuditLogger(
            IOptions<SecurityConfiguration> securityConfiguration,
            ILogger<IAuditLogger> logger)
        {
            EnsureArg.IsNotNull(securityConfiguration?.Value, nameof(securityConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _securityConfiguration = securityConfiguration.Value;
            _logger = logger;
        }

        /// <inheritdoc />
        public void LogAudit(
            AuditAction auditAction,
            string action,
            Uri requestUri,
            HttpStatusCode? statusCode,
            string correlationId,
            string callerIpAddress,
            IReadOnlyCollection<KeyValuePair<string, string>> callerClaims,
            IReadOnlyDictionary<string, string> customerHeaders = null,
            Exception exception = null)
        {
            string claimsInString = null;
            string customerHeadersInString = null;
            string exceptionMessage = null;

            if (callerClaims != null)
            {
                claimsInString = string.Join(";", callerClaims.Select(claim => $"{claim.Key}={claim.Value}"));
            }

            if (customerHeaders != null)
            {
                customerHeadersInString = string.Join(";", customerHeaders.Select(header => $"{header.Key}={header.Value}"));
            }

            if (exception != null)
            {
                auditMessageFormat = string.Concat(auditMessageFormat, Environment.NewLine, "Exception: {Exception}");
                exceptionMessage = string.Concat(exception.Message, Environment.NewLine, exception.StackTrace);
            }

            _logger.LogInformation(
                auditMessageFormat,
                auditAction,
                AuditEventType,
                _securityConfiguration.Authentication?.Audience,
                _securityConfiguration.Authentication?.Authority,
                requestUri,
                action,
                statusCode,
                correlationId,
                claimsInString,
                customerHeadersInString,
                exceptionMessage);
        }
    }
}
