// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Core.Features.Security;

namespace Microsoft.Health.Dicom.Api.Features.Audit
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AuditLoggingFilterAttribute : Microsoft.Health.Api.Features.Audit.AuditLoggingFilterAttribute
    {
        public AuditLoggingFilterAttribute(
            IClaimsExtractor claimsExtractor,
            IAuditHelper auditHelper)
            : base(claimsExtractor, auditHelper)
        {
        }

        /// <summary>
        /// Do nothing when execution finishes for a request.
        /// LogExecuted will be called from AuditMiddleware.
        /// AuditEgressLogger is responsible for taking care of this.
        /// </summary>
        /// <param name="context">On result executed context.</param>
        public override void OnResultExecuted(ResultExecutedContext context)
        {
            // Do nothing.
        }
    }
}
