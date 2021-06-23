// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Extensions.DependencyInjection
{
    public interface ISqlServiceBuilder
    {
        IServiceCollection Services { get; }
    }

    internal sealed class SqlServiceBuilder : ISqlServiceBuilder
    {
        public IServiceCollection Services { get; }

        public SqlServiceBuilder(IServiceCollection serviceCollection)
            => Services = EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
    }
}
