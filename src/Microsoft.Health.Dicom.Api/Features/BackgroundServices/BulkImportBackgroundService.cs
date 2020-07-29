// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Dicom.Core.Features.BulkImport;

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices
{
    public class BulkImportBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public BulkImportBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedBackgroundWorker = scope.ServiceProvider.GetRequiredService<BulkImportBackgroundWorker>();

                await scopedBackgroundWorker.ExecuteAsync(stoppingToken);
            }
        }
    }
}
