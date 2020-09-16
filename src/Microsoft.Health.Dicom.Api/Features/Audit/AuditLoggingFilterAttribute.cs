// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
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

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            if (context.Exception != null)
            {
                AuditHelper.LogExecuted(context.HttpContext, ClaimsExtractor);
            }

            base.OnActionExecuted(context);
        }
    }
}
