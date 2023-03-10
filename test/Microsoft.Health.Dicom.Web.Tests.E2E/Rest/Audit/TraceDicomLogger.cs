// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Diagnostic;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest.Audit;

public class TraceDicomLogger : IDicomLogger
{
    private readonly List<AuditEntry> _auditEntries = new List<AuditEntry>();
    private readonly object _syncLock = new object();

    public void LogAudit(
        AuditAction auditAction,
        IReadOnlyDictionary<string, string> customHeaders = null)
    {
        lock (_syncLock)
        {
            _auditEntries.Add(new AuditEntry(auditAction));
        }
    }

    public void LogDiagnostic(string message, InstanceIdentifier instanceIdentifier) => throw new NotImplementedException();

    public IReadOnlyList<AuditEntry> GetAuditEntries()
    {
        lock (_syncLock)
        {
            return _auditEntries;
        }
    }
}
