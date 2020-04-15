// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Delete;

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices
{
    public class DeletedInstanceCleanupWorker
    {
        private readonly ILogger<DeletedInstanceCleanupWorker> _logger;
        private readonly IDicomDeleteService _deleteService;
        private BackgroundCleanupConfiguration _backgroundCleanupConfiguration;
        private readonly TimeSpan _pollingInterval;

        public DeletedInstanceCleanupWorker(IDicomDeleteService deleteService, IOptions<BackgroundCleanupConfiguration> backgroundCleanupConfiguration, ILogger<DeletedInstanceCleanupWorker> logger)
        {
            EnsureArg.IsNotNull(deleteService, nameof(deleteService));
            EnsureArg.IsNotNull(backgroundCleanupConfiguration?.Value, nameof(backgroundCleanupConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _deleteService = deleteService;
            _backgroundCleanupConfiguration = backgroundCleanupConfiguration.Value;
            _pollingInterval = TimeSpan.FromMinutes(_backgroundCleanupConfiguration.PollingInterval);
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    bool success;
                    int rowsProcessed;
                    do
                    {
                        (success, rowsProcessed) = await _deleteService.CleanupDeletedInstancesAsync(stoppingToken);

                        // TODO:  Add logging
                    }
                    while (success && rowsProcessed > 0);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Cancel requested.
                }
                catch (Exception ex)
                {
                    // The job failed.
                    _logger.LogError(ex, "Unhandled exception in the worker.");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }
    }
}
