// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.DicomCast.Core.Modules;

public class DicomModule : IStartupModule
{
    private readonly IConfiguration _configuration;

    public DicomModule(IConfiguration configuration)
    {
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        _configuration = configuration;
    }

    public void Load(IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));
        services.AddDicomModule(_configuration);
    }
}
