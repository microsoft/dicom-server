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

    public static IDicomServerBuilder AddDicomServer(this IServiceCollection services,
        IConfiguration configurationRoot,
        Action<DicomServerConfiguration> configureAction = null)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        var dicomServerConfiguration = new DicomServerConfiguration();

        configurationRoot?.GetSection(DicomServerConfigurationSectionName).Bind(dicomServerConfiguration);
        configureAction?.Invoke(dicomServerConfiguration);

        services.AddSingleton(Options.Create(dicomServerConfiguration));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Security));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Features));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.DeletedInstanceCleanup));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.StoreServiceSettings));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.ExtendedQueryTag));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.DataPartition));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Audit));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.RetrieveConfiguration));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.BlobMigration));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.InstanceMetadataCacheConfiguration));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.FramesRangeCacheConfiguration));

        services.TryAddSingleton<RecyclableMemoryStreamManager>();

        services.RegisterAssemblyModules(typeof(MediationModule).Assembly, dicomServerConfiguration, dicomServerConfiguration.Features, dicomServerConfiguration.Services);

        return new DicomServerBuilder(services, dicomServerConfiguration);
    }

    private class DicomServerBuilder : IDicomServerBuilder
    {
        public DicomServerBuilder(IServiceCollection services, DicomServerConfiguration configuration)
        {
            Services = EnsureArg.IsNotNull(services, nameof(services));
            DicomServerConfiguration = EnsureArg.IsNotNull(configuration, nameof(configuration));
        }

        public IServiceCollection Services { get; }
        public DicomServerConfiguration DicomServerConfiguration { get; }
    }
}
