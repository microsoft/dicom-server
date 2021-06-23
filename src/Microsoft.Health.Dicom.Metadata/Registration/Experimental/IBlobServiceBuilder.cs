// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IBlobServiceBuilder
    {
        IServiceCollection Services { get; }
    }

    internal sealed class BlobServiceBuilder : IBlobServiceBuilder
    {
        public IServiceCollection Services { get; }

        public BlobServiceBuilder(IServiceCollection serviceCollection)
            => Services = EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
    }
}
