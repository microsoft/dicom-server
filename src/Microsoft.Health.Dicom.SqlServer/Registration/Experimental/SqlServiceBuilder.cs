// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class SqlServiceBuilder : ISqlServiceBuilder
    {
        public IServiceCollection Services { get; }

        public SqlServerDataStoreConfiguration Configuration { get; }

        SqlServerDataStoreConfiguration ISqlServiceBuilder.Configuration => throw new NotImplementedException();

        public SqlServiceBuilder(IServiceCollection serviceCollection, SqlServerDataStoreConfiguration config)
        {
            Services = EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
            Configuration = EnsureArg.IsNotNull(config, nameof(config));
        }
    }
}
