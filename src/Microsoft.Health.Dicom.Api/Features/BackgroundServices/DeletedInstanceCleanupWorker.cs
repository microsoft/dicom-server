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
        private readonly TimeSpan _pollingInterval;
        private readonly int _batchSize;

        public DeletedInstanceCleanupWorker(IDicomDeleteService deleteService, IOptions<DeletedInstanceCleanupConfiguration> backgroundCleanupConfiguration, ILogger<DeletedInstanceCleanupWorker> logger)
        {
            EnsureArg.IsNotNull(deleteService, nameof(deleteService));
            EnsureArg.IsNotNull(backgroundCleanupConfiguration?.Value, nameof(backgroundCleanupConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _deleteService = deleteService;
            _pollingInterval = backgroundCleanupConfiguration.Value.PollingInterval;
            _batchSize = backgroundCleanupConfiguration.Value.BatchSize;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    bool success;
                    int retrievedInstanceCount;
                    do
                    {
                        (success, retrievedInstanceCount) = await _deleteService.CleanupDeletedInstancesAsync(stoppingToken);
                    }
                    while (success && retrievedInstanceCount == _batchSize);

                    await Task.Delay(_pollingInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Cancel requested.
                    break;
                }
                catch (Exception ex)
                {
                    // The job failed.
                    _logger.LogCritical(ex, "Unhandled exception in the deleted instance cleanup worker.");
                }
            }
        }
    }
}
