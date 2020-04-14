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

            services.Add<DicomStoreService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomStorePersistenceOrchestrator>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Decorate<IDicomStorePersistenceOrchestrator, LoggingDicomStorePersistenceOrchestrator>();

            services.Add<DicomInstanceEntryReaderForMultipartRequest>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Decorate<IDicomInstanceEntryReader, LoggingDicomInstanceEntryReader>();

            services.Add<DicomStoreResponseBuilder>()
                .Transient()
                .AsSelf()
                .AsFactory();

            services.Add<DicomRetrieveMetadataService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomRetrieveResourceService>()
                .Scoped()
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
