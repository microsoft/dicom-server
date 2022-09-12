// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Modules;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Registration;

public static class DicomServerApplicationBuilderExtensions
{
    private const string DicomServerConfigurationSectionName = "DicomServer";

    public static IDicomServerBuilder AddDicomCore(this IServiceCollection services,
        IConfiguration configurationRoot,
        Action<CoreConfiguration> configureAction = null)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        var coreConfiguration = new CoreConfiguration();

        configurationRoot?.GetSection(DicomServerConfigurationSectionName).Bind(coreConfiguration);
        configureAction?.Invoke(coreConfiguration);

        services.AddSingleton(Options.Create(coreConfiguration));
        services.AddSingleton(Options.Create(coreConfiguration.Security));
        services.AddSingleton(Options.Create(coreConfiguration.Features));
        services.AddSingleton(Options.Create(coreConfiguration.Services.DeletedInstanceCleanup));
        services.AddSingleton(Options.Create(coreConfiguration.Services.StoreServiceSettings));
        services.AddSingleton(Options.Create(coreConfiguration.Services.ExtendedQueryTag));
        services.AddSingleton(Options.Create(coreConfiguration.Services.DataPartition));
        services.AddSingleton(Options.Create(coreConfiguration.Audit));
        services.AddSingleton(Options.Create(coreConfiguration.Services.RetrieveConfiguration));
        services.AddSingleton(Options.Create(coreConfiguration.Services.BlobMigration));
        services.AddSingleton(Options.Create(coreConfiguration.Services.InstanceMetadataCacheConfiguration));
        services.AddSingleton(Options.Create(coreConfiguration.Services.FramesRangeCacheConfiguration));

        services.TryAddSingleton<RecyclableMemoryStreamManager>();

        services.RegisterAssemblyModules(typeof(MediationModule).Assembly, coreConfiguration, coreConfiguration.Features, coreConfiguration.Services);

        return new DicomServerBuilder(services, coreConfiguration);
    }

    private class DicomServerBuilder : IDicomServerBuilder
    {
        public DicomServerBuilder(IServiceCollection services, CoreConfiguration configuration)
        {
            Services = EnsureArg.IsNotNull(services, nameof(services));
            CoreConfiguration = EnsureArg.IsNotNull(configuration, nameof(configuration));
        }

        public IServiceCollection Services { get; }

        public CoreConfiguration CoreConfiguration { get; }
    }
}
