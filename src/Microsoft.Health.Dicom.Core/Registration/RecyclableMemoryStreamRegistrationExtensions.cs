// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Registration;

public static class RecyclableMemoryStreamRegistrationExtensions
{
    public const string DefaultSectionName = "RecyclableMemoryStream";

    public static IServiceCollection AddRecyclableMemoryStreamManager(this IServiceCollection services, IConfiguration configuration)
    {
        EnsureArg.IsNotNull(configuration, nameof(configuration));
        return services.AddRecyclableMemoryStreamManager(
            o => configuration
                .GetSection(DefaultSectionName)
                .Bind(o));
    }

    public static IServiceCollection AddRecyclableMemoryStreamManager(this IServiceCollection services, Action<RecyclableMemoryStreamManager.Options> configure)
    {
        EnsureArg.IsNotNull(configure, nameof(configure));
        return services
            .AddRecyclableMemoryStreamManager()
            .Configure(configure);
    }

    public static IServiceCollection AddRecyclableMemoryStreamManager(this IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));
        return services.AddSingleton(
            sp =>
            {
                RecyclableMemoryStreamManager.Options options = sp.GetRequiredService<IOptions<RecyclableMemoryStreamManager.Options>>().Value;
                return new RecyclableMemoryStreamManager(options);
            });
    }
}
