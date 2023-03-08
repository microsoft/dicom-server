// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.Health.Dicom.Api.Features.Audit;

[AttributeUsage(AttributeTargets.Class)]
[SuppressMessage("Performance", "CA1813:Avoid unsealed attributes", Justification = "This attribute to meant to be extended.")]
public class AuditLoggingFilterAttribute : ActionFilterAttribute
{
    public AuditLoggingFilterAttribute(
        IAuditHelper auditHelper)
    {
        EnsureArg.IsNotNull(auditHelper, nameof(auditHelper));

        AuditHelper = auditHelper;
    }

    protected IAuditHelper AuditHelper { get; }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        // AuditHelper.LogExecuting(context.HttpContext);
        // todo will remove whole filter later once paas fully converted

        base.OnActionExecuting(context);
    }
}
