// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.SqlServer.Configs;
using Microsoft.Health.Fhir.SqlServer.Features.Health;
using Microsoft.Health.Fhir.SqlServer.Features.Query;
using Microsoft.Health.Fhir.SqlServer.Features.Schema;
using Microsoft.Health.Fhir.SqlServer.Features.Storage;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomServerBuilderSqlServerRegistrationExtensions
    {
        public static IDicomServerBuilder AddSqlServer(this IDicomServerBuilder dicomServerBuilder, Action<SqlServerDataStoreConfiguration> configureAction = null)
        {
            EnsureArg.IsNotNull(dicomServerBuilder, nameof(dicomServerBuilder));
            IServiceCollection services = dicomServerBuilder.Services;

            services.Add(provider =>
                {
                    var config = new SqlServerDataStoreConfiguration();
                    provider.GetService<IConfiguration>().GetSection("SqlServer").Bind(config);
                    configureAction?.Invoke(config);

                    return config;
                })
                .Singleton()
                .AsSelf();

            services.Add<SchemaUpgradeRunner>()
                .Singleton()
                .AsSelf();

            services.Add<SchemaInformation>()
                .Singleton()
                .AsSelf();

            services.Add<SchemaInitializer>()
                .Singleton()
                .AsService<IStartable>();

            services.Add<SqlTransactionHandler>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlConnectionWrapperFactory>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlServerDicomIndexSchema>()
                .Singleton()
                .AsSelf();

            services
                .AddHealthChecks()
                .AddCheck<SqlServerHealthCheck>(nameof(SqlServerHealthCheck));

            services.Add<SqlServerDicomIndexDataStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomSqlQueryService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return dicomServerBuilder;
        }
    }
}
