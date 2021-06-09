// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.HealthCheck;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace Microsoft.Health.Dicom.Core.Modules
{
    public class ServiceModule : IStartupModule
    {
        private readonly FeatureConfiguration _featureConfiguration;
        private readonly OperationsConfiguration _operationsConfiguration;

        public ServiceModule(FeatureConfiguration featureConfiguration, OperationsConfiguration operationsConfiguration)
        {
            EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));
            EnsureArg.IsNotNull(operationsConfiguration, nameof(operationsConfiguration));

            _featureConfiguration = featureConfiguration;
            _operationsConfiguration = operationsConfiguration;
        }

        public void Load(IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.Add<DicomInstanceEntryReaderManager>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomDatasetValidator>()
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

            services.Add<QueryTagService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            IEnumerable<TimeSpan> delays = Backoff.ExponentialBackoff(
                TimeSpan.FromMilliseconds(_operationsConfiguration.MinRetryDelayMilliseconds),
                _operationsConfiguration.MaxRetries);

            services.AddHttpClient<DicomDurableFunctionsHttpClient>()
                .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(delays));

            services.Add<DicomDurableFunctionsHttpClient>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<OperationsService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<OperationStatusHandler>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.AddSingleton<BackgroundServiceHealthCheckCache>();

            services.AddHealthChecks().AddCheck<BackgroundServiceHealthCheck>(name: "BackgroundServiceHealthCheck");

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
            }
        }
    }
}
