// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.State;
using Microsoft.Health.DicomCast.Core.Features.Worker;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.DicomCast.Core.Modules
{
    public class WorkerModule : IStartupModule
    {
        private const string DicomCastWorkerConfigurationSectionName = "DicomCastWorker";
        private const string DicomValidationConfigurationSectionName = "DicomValidation";

        private readonly IConfiguration _configuration;

        public WorkerModule(IConfiguration configuration)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            _configuration = configuration;
        }

        public void Load(IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            DicomCastWorkerConfiguration dicomCastWorkerConfiguration = services.Configure<DicomCastWorkerConfiguration>(
                _configuration,
                DicomCastWorkerConfigurationSectionName);

            DicomValidationConfiguration dicomValidationConfiguration = services.Configure<DicomValidationConfiguration>(
                _configuration,
                DicomValidationConfigurationSectionName);

            services.Add<DicomCastWorker>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            RegisterPipeline(services);

            services.Add<ChangeFeedProcessor>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Decorate<IChangeFeedProcessor, LoggingChangeFeedProcessor>();

            services.Add<SyncStateService>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();
        }

        private static void RegisterPipeline(IServiceCollection services)
        {
            services.Add<FhirTransactionPipeline>()
                .Transient()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Decorate<IFhirTransactionPipeline, RetryableFhirTransactionPipeline>();
            services.Decorate<IFhirTransactionPipeline, LoggingFhirTransactionPipeline>();

            services.Add<Func<IFhirTransactionPipeline>>(sp => () => sp.GetRequiredService<IFhirTransactionPipeline>())
                .Transient()
                .AsSelf();

            RegisterPipelineSteps(services);

            services.Decorate<IFhirTransactionPipelineStep, LoggingFhirTransactionPipelineStep>();

            services.Add<PatientSynchronizer>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<PatientNameSynchronizer>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<PatientGenderSynchronizer>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<PatientBirthDateSynchronizer>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<ImagingStudySynchronizer>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<ImagingStudyPropertySynchronizer>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<ImagingStudySeriesPropertySynchronizer>()
               .Singleton()
               .AsSelf()
               .AsImplementedInterfaces();

            services.Add<ImagingStudyInstancePropertySynchronizer>()
               .Singleton()
               .AsSelf()
               .AsImplementedInterfaces();

            services.Add<FhirTransactionRequestResponsePropertyAccessors>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();
        }

        private static void RegisterPipelineSteps(IServiceCollection services)
        {
            // The order matters for the following pipeline steps.
            services.Add<PatientPipelineStep>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<EndpointPipelineStep>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<ImagingStudyPipelineStep>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();
        }
    }
}
