// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest.Audit
{
    public class TraceAuditLogger : IAuditLogger
    {
        private readonly List<AuditEntry> _auditEntries = new List<AuditEntry>();
        private readonly object _syncLock = new object();

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
            lock (_syncLock)
            {
                _auditEntries.Add(new AuditEntry(auditAction, operation, requestUri, statusCode));
            }
        }

        public IReadOnlyList<AuditEntry> GetAuditEntriesByOperationAndRequestUri(string operation, Uri uri)
        {
            lock (_syncLock)
            {
                return _auditEntries.Where(ae => string.Equals(ae.Action, operation) && string.Equals(ae.RequestUri?.ToString(), uri?.ToString())).ToList();
            }
        }
    }
}
