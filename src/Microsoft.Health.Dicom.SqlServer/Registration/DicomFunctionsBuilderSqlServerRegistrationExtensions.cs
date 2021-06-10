// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.SqlServer.Features.Indexing;
using Microsoft.Health.Dicom.SqlServer.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.SqlServer.Features.Storage;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomFunctionsBuilderSqlServerRegistrationExtensions
    {
        public static IDicomFunctionsBuilder AddSqlServer(
            this IDicomFunctionsBuilder builder,
            IConfiguration configurationRoot)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));
            IServiceCollection services = builder.Services;
            var config = new SqlServerDataStoreConfiguration();
            configurationRoot?.GetSection("SqlServer").Bind(config);

            SchemaInformation schemaInformation = new SchemaInformation(SchemaVersionConstants.Min, SchemaVersionConstants.Max);
            services.AddSingleton(schemaInformation);
            services.AddSingleton(Options.Options.Create(config));

            AddInitializedSqlServerBase(services, config);

            services.AddScopedDefault<SqlInstanceStore>();
            services.AddScopedDefault<SqlExtendedQueryTagStoreV1>();
            services.AddScopedDefault<SqlExtendedQueryTagStoreV2>();
            services.AddScopedDefault<SqlExtendedQueryTagStoreV3>();
            services.AddScopedDefault<SqlStoreFactory<ISqlExtendedQueryTagStore, IExtendedQueryTagStore>>();
            services.AddScopedDefault<SqlReindexStore>();
            return builder;
        }

        private static IServiceCollection AddInitializedSqlServerBase(
           this IServiceCollection services,
           SqlServerDataStoreConfiguration configuration)
        {
            //  SqlServerDataStoreConfiguration is consumed by DefaultSqlConnectionStringProvider
            services.AddSingleton(configuration);

            // TODO: consider moving these logic into healthcare-shared-components (https://github.com/microsoft/healthcare-shared-components/)
            // once code becomes solid (e.g: merging back to main branch).                 
            services.AddScopedDefault<SqlTransactionHandler>();
            services.AddScopedDefault<SqlConnectionWrapperFactory>();
            services.AddSingletonDefault<SchemaManagerDataStore>();
            // TODO:  Use RetrySqlCommandWrapperFactory instead when moving to healthcare-shared-components 
            services.AddSingletonDefault<SqlCommandWrapperFactory>();
            services.AddSingletonDefault<DefaultSqlConnectionStringProvider>();
            services.AddSingletonDefault<DefaultSqlConnectionFactory>();

            return services;
        }
    }
}
