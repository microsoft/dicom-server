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

            AddInitializedSqlServerBase(services, config);
            services.AddSingleton(Options.Options.Create(config));
            services.AddSingletonDefault<SqlInstanceStore>();
            services.AddSingletonDefault<SqlExtendedQueryTagStoreV1>();
            services.AddSingletonDefault<SqlExtendedQueryTagStoreV2>();
            services.AddSingletonDefault<SqlExtendedQueryTagStoreV3>();
            services.AddSingletonDefault<SqlStoreFactory<ISqlExtendedQueryTagStore, IExtendedQueryTagStore>>();
            services.AddSingletonDefault<SqlReindexStore>();
            return builder;
        }

        private static IServiceCollection AddInitializedSqlServerBase(
           this IServiceCollection services,
           SqlServerDataStoreConfiguration configuration)
        {
            // TODO: consider moving these logic into healthcare-shared-components (https://github.com/microsoft/healthcare-shared-components/)
            // once code becomes solid (e.g: merging back to main branch).
            services.AddSingletonDefault<SchemaManagerDataStore>();
            services.AddScoped<SqlTransactionHandler>();
            // TODO:  Use RetrySqlCommandWrapperFactory instead when moving to healthcare-shared-components 
            services.AddSingletonDefault<SqlCommandWrapperFactory>();

            //  SqlServerDataStoreConfiguration is consumed by DefaultSqlConnectionStringProvider
            services.AddSingleton(configuration);
            services.AddSingletonDefault<DefaultSqlConnectionStringProvider>();
            services.AddSingletonDefault<DefaultSqlConnectionFactory>();
            services.AddScoped<SqlConnectionWrapperFactory>();
            return services;
        }
    }
}
