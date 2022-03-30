// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.HealthCheck;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Dicom.Core.Modules;

public class ServiceModule : IStartupModule
{
    private readonly FeatureConfiguration _featureConfiguration;

    public ServiceModule(FeatureConfiguration featureConfiguration)
    {
        EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));
        _featureConfiguration = featureConfiguration;
    }

    public void Load(IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        services.AddFellowOakDicomServices(skipValidation: true);

        services.Add<DicomInstanceEntryReaderManager>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<StoreDatasetValidator>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<StoreService>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces()
            .AsFactory();

        services.Add<StoreOrchestrator>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Decorate<IStoreOrchestrator, LoggingStoreOrchestrator>();

        services.Add<DicomInstanceEntryReaderForMultipartRequest>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<DicomInstanceEntryReaderForSinglePartRequest>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Decorate<IDicomInstanceEntryReader, LoggingDicomInstanceEntryReader>();

        services.Add<DicomInstanceEntryReaderManager>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Decorate<IDicomInstanceEntryReaderManager, LoggingDicomInstanceEntryReaderManager>();

        services.Add<StoreResponseBuilder>()
            .Transient()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<RetrieveMetadataService>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<RetrieveResourceService>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<FrameHandler>()
            .Transient()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<RetrieveTransferSyntaxHandler>()
            .Transient()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<Transcoder>()
            .Transient()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<QueryService>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<DicomTagParser>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

        services.AddTransient<IQueryParser<QueryExpression, QueryParameters>, QueryParser>();
        services.AddTransient<IQueryParser<BaseQueryExpression, BaseQueryParameters>, WorkitemQueryParser>();

        services.Add<DeleteService>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<ChangeFeedService>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<ElementMinimumValidator>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<ETagGenerator>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<QueryTagService>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<OperationStateHandler>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.AddSingleton<BackgroundServiceHealthCheckCache>();

        services.AddHealthChecks().AddCheck<BackgroundServiceHealthCheck>(name: "BackgroundServiceHealthCheck");

        services.AddSingleton<PartitionCache>();

        services.Add<PartitionService>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<WorkitemQueryTagService>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        if (_featureConfiguration.EnableExtendedQueryTags)
        {
            services.Add<ExtendedQueryTagEntryValidator>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<GetExtendedQueryTagsService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<AddExtendedQueryTagService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DeleteExtendedQueryTagService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<UpdateExtendedQueryTagService>()
             .Scoped()
             .AsSelf()
             .AsImplementedInterfaces();

            services.Add<ExtendedQueryTagErrorsService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<ReindexDatasetValidator>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<InstanceReindexer>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();
        }

        if (_featureConfiguration.EnableExport)
        {
            services.Add<ExportService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();
        }

        SetupWorkitemTypes(services);
        RegisterExportServices(services);
    }

    private static void SetupWorkitemTypes(IServiceCollection services)
    {
        services.Add<WorkitemService>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<WorkitemSerializer>()
           .Scoped()
           .AsSelf()
           .AsImplementedInterfaces();

        services.Add<WorkitemOrchestrator>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<WorkitemResponseBuilder>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<AddWorkitemDatasetValidator>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<CancelWorkitemDatasetValidator>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();
    }

    private static void RegisterExportServices(IServiceCollection services)
    {
        // Sources
        //services.AddSingleton<IExportSourceFactory, ExportSourceFactory>();

        // Sinks
        services.AddSingleton<IExportSinkFactory, ExportSinkFactory>();
    }
}
