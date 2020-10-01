// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Core.Features.Security;

namespace Microsoft.Health.Dicom.Api.Features.Audit
{
    /// <summary>
    /// Audit egress logger for DICOM that logs all the executed messages.
    /// </summary>
    public class AuditEgressLogger : IAuditEgressLogger
    {
        /// <summary>
        /// Log all the executed messages.
        /// </summary>
        /// <param name="httpContext">Http context.</param>
        /// <param name="claimsExtractor">Claims extractor.</param>
        /// <param name="auditHelper">Audit helper.</param>
        public void LogExecuted(HttpContext httpContext, IClaimsExtractor claimsExtractor, IAuditHelper auditHelper)
        {
            EnsureArg.IsNotNull(httpContext, nameof(httpContext));
            EnsureArg.IsNotNull(claimsExtractor, nameof(claimsExtractor));
            EnsureArg.IsNotNull(auditHelper, nameof(auditHelper));

            auditHelper.LogExecuted(httpContext, claimsExtractor);
        }
    }
}
