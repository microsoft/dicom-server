// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using Microsoft.Health.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest.Audit
{
    public class AuditEntry
    {
        public AuditEntry(
            AuditAction auditAction,
            string action,
            Uri requestUri,
            HttpStatusCode? statusCode)
        {
            AuditAction = auditAction;
            Action = action;
            RequestUri = requestUri;
            StatusCode = statusCode;
        }

        public AuditAction AuditAction { get; }

        public string Action { get; }

        public Uri RequestUri { get; }

        public HttpStatusCode? StatusCode { get; }
    }
}
