// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest.Audit
{
    public class TraceAuditLogger : IAuditLogger
    {
        private BlockingCollection<AuditEntry> _auditEntries = new BlockingCollection<AuditEntry>();

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
            _auditEntries.Add(new AuditEntry(auditAction, operation, requestUri, statusCode));
        }

        public IReadOnlyList<AuditEntry> GetAuditEntriesByOperationAndRequestUri(string operation, Uri uri)
        {
            return _auditEntries.Where(ae => string.Equals(ae.Action, operation) && string.Equals(ae.RequestUri?.ToString(), uri?.ToString())).ToList();
        }
    }
}
