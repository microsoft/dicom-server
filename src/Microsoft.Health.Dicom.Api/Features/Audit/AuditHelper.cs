// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Core.Features.Context;
using Microsoft.Health.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Dicom.Api.Features.Audit
{
    public class AuditHelper : IAuditHelper
    {
        private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
        private readonly IAuditLogger _auditLogger;
        private readonly IAuditHeaderReader _auditHeaderReader;

        public AuditHelper(
            IDicomRequestContextAccessor dicomRequestContextAccessor,
            IAuditLogger auditLogger,
            IAuditHeaderReader auditHeaderReader)
        {
            EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
            EnsureArg.IsNotNull(auditLogger, nameof(auditLogger));
            EnsureArg.IsNotNull(auditHeaderReader, nameof(auditHeaderReader));

            _dicomRequestContextAccessor = dicomRequestContextAccessor;
            _auditLogger = auditLogger;
            _auditHeaderReader = auditHeaderReader;
        }

        /// <inheritdoc />
        public void LogExecuting(HttpContext httpContext, IClaimsExtractor claimsExtractor)
        {
            EnsureArg.IsNotNull(claimsExtractor, nameof(claimsExtractor));
            EnsureArg.IsNotNull(httpContext, nameof(httpContext));

            Log(AuditAction.Executing, statusCode: null, httpContext, claimsExtractor);
        }

        /// <summary>
        /// Logs an executed audit entry for the current operation.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <param name="claimsExtractor">The extractor used to extract claims.</param>
        /// <param name="shouldCheckForAuthXFailure">
        /// Should check for AuthX failure and print LogExecuted messages only if it is AuthX failure.
        /// This is no-op in DICOM as all the log executed messages are written at one place.
        /// </param>
        /// </summary>
        public void LogExecuted(HttpContext httpContext, IClaimsExtractor claimsExtractor, bool shouldCheckForAuthXFailure)
        {
            EnsureArg.IsNotNull(claimsExtractor, nameof(claimsExtractor));
            EnsureArg.IsNotNull(httpContext, nameof(httpContext));

            Log(AuditAction.Executed, (HttpStatusCode)httpContext.Response.StatusCode, httpContext, claimsExtractor);
        }

        private void Log(AuditAction auditAction, HttpStatusCode? statusCode, HttpContext httpContext, IClaimsExtractor claimsExtractor)
        {
            IRequestContext dicomRequestContext = _dicomRequestContextAccessor.DicomRequestContext;

            string auditEventType = dicomRequestContext.AuditEventType;

            // Audit the call if an audit event type is associated with the action.
            if (!string.IsNullOrEmpty(auditEventType))
            {
                _auditLogger.LogAudit(
                    auditAction,
                    operation: auditEventType,
                    requestUri: dicomRequestContext.Uri,
                    statusCode: statusCode,
                    correlationId: dicomRequestContext.CorrelationId,
                    callerIpAddress: httpContext.Connection?.RemoteIpAddress?.ToString(),
                    callerClaims: claimsExtractor.Extract(),
                    customHeaders: _auditHeaderReader.Read(httpContext));
            }
        }
    }
}
