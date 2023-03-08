// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Dicom.Api.Features.Audit;

// this moved to paas
public class AuditHelper : IAuditHelper
{
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly IDicomLogger _dicomLogger;
    private readonly IAuditHeaderReader _auditHeaderReader;

    public AuditHelper(
        IDicomRequestContextAccessor dicomRequestContextAccessor,
        IDicomLogger dicomLogger,
        IAuditHeaderReader auditHeaderReader)
    {
        EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
        EnsureArg.IsNotNull(dicomLogger, nameof(dicomLogger));
        EnsureArg.IsNotNull(auditHeaderReader, nameof(auditHeaderReader));

        _dicomRequestContextAccessor = dicomRequestContextAccessor;
        _dicomLogger = dicomLogger;
        _auditHeaderReader = auditHeaderReader;
    }

    /// <inheritdoc />
    public void LogExecuting(HttpContext httpContext)
    {
    }

    /// <summary>
    /// Logs an executed audit entry for the current operation.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    public void LogExecuted(HttpContext httpContext)
    {
    }
}
