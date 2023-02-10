// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Dicom.Core.Modules;

public class ServiceModule : IStartupModule
{
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

        services.Add<DicomInstanceEntryReaderForMultipartRequest>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<DicomInstanceEntryReaderForSinglePartRequest>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<DicomInstanceEntryReaderManager>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

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

        services.Add<AcceptHeaderHandler>()
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

        services.AddSingleton<IGuidFactory>(GuidFactory.Default);

        services.AddScoped<IDicomOperationsResourceStore, DicomOperationsResourceStore>();

        services.AddSingleton<BackgroundServiceHealthCheckCache>();

        services.AddHealthChecks().AddCheck<BackgroundServiceHealthCheck>(name: "BackgroundServiceHealthCheck");

        services.AddSingleton<PartitionCache>();

        services.Add<PartitionService>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<InstanceMetadataCache>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<FramesRangeCache>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

        services.AddSingleton<DeleteMeter>();
        services.AddSingleton<RetrieveMeter>();
        services.AddSingleton<StoreMeter>();

        AddExtendedQueryTagServices(services);

        AddWorkItemServices(services);

        AddExportServices(services);
    }

    private static void AddExtendedQueryTagServices(IServiceCollection services)
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

    private static void AddWorkItemServices(IServiceCollection services)
    {
        services.Add<WorkitemQueryTagService>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<WorkitemService>()
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

        services.Add<ChangeWorkitemStateDatasetValidator>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        services.Add<UpdateWorkitemDatasetValidator>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();
    }

    private static void AddExportServices(IServiceCollection services)
    {
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<ExportSourceFactory>();
        services.AddScoped<ExportSinkFactory>();
        services.TryAddScoped<IExternalOperationCredentialProvider, DefaultExternalOperationCredentialProvider>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IExportSourceProvider, IdentifierExportSourceProvider>());
    }
}
