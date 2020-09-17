// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Dicom.Core.Modules
{
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

            services.Add<DicomInstanceEntryReaderManager>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomDatasetValidator>()
                .Singleton()
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

            services.AddTransient<IQueryParser, QueryParser>();

            services.Add<DeleteService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<ChangeFeedService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomElementMinimumValidator>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<ETagGenerator>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();
        }
    }
}
