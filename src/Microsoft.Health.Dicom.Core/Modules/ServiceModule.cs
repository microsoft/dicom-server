// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Dicom.Core.Modules
{
    public class ServiceModule : IStartupModule
    {
        public void Load(IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.Add<DicomInstanceEntryReaderManager>()
                .Singleton()
                .AsImplementedInterfaces();

            services.Add<DicomDatasetMinimumRequirementValidator>()
                .Singleton()
                .AsImplementedInterfaces();

            services.Add<DicomStoreService>()
                .Scoped()
                .AsImplementedInterfaces()
                .AsFactory();

            services.Add<DicomStoreOrchestrator>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Decorate<IDicomStoreOrchestrator, LoggingDicomStoreOrchestrator>();

            services.Add<DicomInstanceEntryReaderForMultipartRequest>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Decorate<IDicomInstanceEntryReader, LoggingDicomInstanceEntryReader>();

            services.Add<DicomInstanceEntryReaderManager>()
                .Singleton()
                .AsImplementedInterfaces();

            services.Decorate<IDicomInstanceEntryReaderManager, LoggingDicomInstanceEntryReaderManager>();

            services.Add<DicomStoreResponseBuilder>()
                .Transient()
                .AsImplementedInterfaces();

            services.Add<DicomRetrieveMetadataService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomRetrieveResourceService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomFrameHandler>()
                .Transient()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomRetrieveTranscoder>()
                .Transient()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomQueryService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.AddTransient<IDicomQueryParser, DicomQueryParser>();

            services.Add<DicomDeleteService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();
        }
    }
}
