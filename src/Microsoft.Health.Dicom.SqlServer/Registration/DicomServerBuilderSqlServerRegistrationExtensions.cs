// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.SqlServer.Features.Query;
using Microsoft.Health.Dicom.SqlServer.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Storage;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Api.Registration;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Registration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomServerBuilderSqlServerRegistrationExtensions
    {
        public static IDicomServerBuilder AddSqlServer(this IDicomServerBuilder dicomServerBuilder, Action<SqlServerDataStoreConfiguration> configureAction = null)
        {
            EnsureArg.IsNotNull(dicomServerBuilder, nameof(dicomServerBuilder));
            IServiceCollection services = dicomServerBuilder.Services;

            services.AddSqlServerBase<SchemaVersion>();
            services.AddSqlServerApi();

            services.Add(provider =>
                {
                    var config = new SqlServerDataStoreConfiguration();
                    provider.GetService<IConfiguration>().GetSection("SqlServer").Bind(config);
                    configureAction?.Invoke(config);

                    return config;
                })
                .Singleton()
                .AsSelf();

            services.Add(provider => new SchemaInformation((int)SchemaVersion.V1, (int)SchemaVersion.V1))
                .Singleton()
                .AsSelf();

            services.Add<DicomSqlIndexSchema>()
                .Singleton()
                .AsSelf();

            services.Add<DicomSqlIndexDataStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomSqlQueryStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomSqlInstanceStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return dicomServerBuilder;
        }
    }
}
