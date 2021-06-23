// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IBlobServiceBuilder
    {
        IServiceCollection Services { get; }

        BlobDataStoreConfiguration Configuration { get; }
    }

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
