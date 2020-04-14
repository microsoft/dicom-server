// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Delete;

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices
{
    public class DeletedInstanceCleanupWorker
    {
        private readonly ILogger<DeletedInstanceCleanupWorker> _logger;
        private readonly IDicomDeleteService _deleteService;
        private const int PollingDelay = 100000;

        public DeletedInstanceCleanupWorker(IDicomDeleteService deleteService, ILogger<DeletedInstanceCleanupWorker> logger)
        {
            EnsureArg.IsNotNull(deleteService, nameof(deleteService));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _deleteService = deleteService;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _deleteService.CleanupDeletedInstancesAsync(stoppingToken);

                    await Task.Delay(PollingDelay, stoppingToken);
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
            }
        }
    }
}
