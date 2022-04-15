// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Dicom.Core.Features.Operations;

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices;

public class StartDuplicatingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public StartDuplicatingBackgroundService(IServiceProvider serviceProvider)
    {
        EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));

        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var client = scope.ServiceProvider.GetRequiredService<IDicomOperationsClient>();

            await client.StartDuplicatingInstancesAsync(stoppingToken);
        }
    }
}
