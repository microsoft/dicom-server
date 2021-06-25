// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Metadata.Features.Health;
using Microsoft.Health.Dicom.Metadata.Features.Storage;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class BlobServiceBuilderExtensions
    {
        public static IBlobServiceBuilder AddMetadataStore(this IBlobServiceBuilder builder)
        {
            IServiceCollection services = EnsureArg.IsNotNull(builder?.Services, nameof(builder));

            services.Add<BlobMetadataStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            // TODO: Ideally, the logger can be registered in the API layer since it's agnostic to the implementation.
            // However, the current implementation of the decorate method requires the concrete type to be already registered,
            // so we need to register here. Need to some more investigation to see how we might be able to do this.
            services.Decorate<IMetadataStore, LoggingMetadataStore>();

            return builder;
        }

        public static IBlobServiceBuilder AddMetadataHealthCheck(this IBlobServiceBuilder builder)
        {
            EnsureArg.IsNotNull(builder?.Services, nameof(builder))
                .AddHealthChecks()
                .AddCheck<MetadataHealthCheck>(name: "MetadataHealthCheck");

            return builder;
        }
    }
}
