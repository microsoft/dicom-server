// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest.Audit
{
    public class AuditTestFixture : HttpIntegrationTestFixture<StartupWithTraceAuditLogger>
    {
        private TraceAuditLogger _auditLogger;

        public AuditTestFixture()
            : base()
        {
        }

        public TraceAuditLogger AuditLogger
        {
            get => _auditLogger ?? (_auditLogger = (TraceAuditLogger)(TestDicomWebServer as InProcTestDicomWebServer)?.Server.Host.Services.GetRequiredService<IAuditLogger>());
        }
    }
}
