// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Models.Delete;

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices;

public class DeletedInstanceCleanupWorker
{
    private readonly ILogger<DeletedInstanceCleanupWorker> _logger;
    private readonly DeleteMeter _deleteMeter;
    private readonly DeletedInstanceCleanupConfiguration _options;
    private readonly IDeleteService _deleteService;

    public DeletedInstanceCleanupWorker(
        IDeleteService deleteService,
        DeleteMeter deleteMeter,
        IOptions<DeletedInstanceCleanupConfiguration> backgroundCleanupConfiguration,
        ILogger<DeletedInstanceCleanupWorker> logger)
    {
        _deleteService = EnsureArg.IsNotNull(deleteService, nameof(deleteService));
        _deleteMeter = EnsureArg.IsNotNull(deleteMeter, nameof(deleteMeter));
        _options = EnsureArg.IsNotNull(backgroundCleanupConfiguration?.Value, nameof(backgroundCleanupConfiguration));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Do not throw exceptions other than those for cancellation.")]
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.PollingInterval, stoppingToken);

                DeleteSummary summary;
                do
                {
                    summary = await _deleteService.CleanupDeletedInstancesAsync(stoppingToken);

                    if (summary.Metrics != null)
                        WriteDeleteMetrics(summary.Metrics.GetValueOrDefault());
                }
                while (summary.Success && summary.ProcessedCount == _options.BatchSize);
            }
            catch (DataStoreNotReadyException)
            {
                _logger.LogInformation("The data store is not currently ready. Processing will continue after the next wait period.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Cancel requested.
                throw;
            }
            catch (Exception ex)
            {
                // The job failed.
                _logger.LogCritical(ex, "Unhandled exception in the deleted instance cleanup worker.");
            }
        }
    }

    private void WriteDeleteMetrics(DeleteMetrics metrics)
    {
        _deleteMeter.OldestRequestedDeletion.Add(metrics.OldestDeletion.ToUnixTimeSeconds());
        _deleteMeter.CountDeletionsMaxRetry.Add(metrics.TotalExhaustedRetries);
    }
}
