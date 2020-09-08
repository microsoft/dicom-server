// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Extensions.DependencyInjection;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Modules
{
    public class AuditModule : IStartupModule
    {
        public void Load(IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.Add<DicomAudit.AuditLoggingFilterAttribute>()
                .Singleton()
                .AsSelf();

            services.AddSingleton<DicomAudit.IAuditLogger, DicomAudit.AuditLogger>();

            services.AddSingleton<IAuditHeaderReader, AuditHeaderReader>();

            services.Add<DicomAudit.AuditHelper>()
                .Singleton()
                .AsService<IAuditHelper>();

            services.Add<AuditEventTypeMapping>()
                .Singleton()
                .AsService<IAuditEventTypeMapping>()
                .AsService<IStartable>();
        }
    }
}
