// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Diagnostic;

/// <summary>
/// Provides mechanism to log audit and diagnostic events meant to be forwarded.
/// </summary>
public interface IDicomForwardingLogger
{
    /// <summary>
    /// Logs an audit event.
    /// </summary>
    /// <param name="auditAction">The action to audit.</param>
    /// <param name="customHeaders">Headers added by the caller with data to be added to the audit logs.</param>
    void LogAudit(
        AuditAction auditAction,
        IReadOnlyDictionary<string, string> customHeaders = null);

    /// <summary>
    /// Logs a diagnostic event.
    /// </summary>
    /// <param name="message">A message containing diagnostic information.</param>
    /// <param name="instanceIdentifier">A dicom dataset's instance identifier.</param>
    public void LogDiagnostic(
        string message,
        InstanceIdentifier instanceIdentifier);
}
