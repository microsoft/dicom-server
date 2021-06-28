// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class BlobServiceBuilder : IBlobServiceBuilder
    {
        public IServiceCollection Services { get; }

        public BlobDataStoreConfiguration Configuration { get; }

        public BlobServiceBuilder(IServiceCollection serviceCollection, BlobDataStoreConfiguration config)
        {
            Services = EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
            Configuration = EnsureArg.IsNotNull(config, nameof(config));
        }
    }
}
