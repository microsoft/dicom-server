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

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices
{
    public class DeletedInstanceCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public DeletedInstanceCleanupBackgroundService(IServiceProvider serviceProvider)
        {
            EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));

            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedDeletedInstanceCleanupWorker = scope.ServiceProvider.GetRequiredService<DeletedInstanceCleanupWorker>();

                await scopedDeletedInstanceCleanupWorker.ExecuteAsync(stoppingToken);
            }
        }
    }
}
