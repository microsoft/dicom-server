// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Extensions.DependencyInjection
{
    public interface ISqlServiceBuilder
    {
        IServiceCollection Services { get; }

        SqlServerDataStoreConfiguration Configuration { get; }
    }
}
