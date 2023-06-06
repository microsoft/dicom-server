// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Api.Controllers;
using Microsoft.Health.SqlServer.Api.Features.Schema;
using Microsoft.Health.SqlServer.Api.Features;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Dicom.SqlServer.Registration;
internal static class SqlServerApiRegistrationOSSExtensions
{
    public static IServiceCollection AddSqlServerApiOSS(this IServiceCollection services)
    {
        EnsureArg.IsNotNull(services);

        services.AddMvc()
            .AddApplicationPart(typeof(SchemaController).Assembly);

        // leave out health check

        services.Add<CompatibilityVersionHandler>()
            .Transient()
            .AsImplementedInterfaces();

        services.Add<CurrentVersionHandler>()
            .Transient()
            .AsImplementedInterfaces();

        services.AddHostedService<SchemaJobWorkerBackgroundService>();

        return services;
    }
}
