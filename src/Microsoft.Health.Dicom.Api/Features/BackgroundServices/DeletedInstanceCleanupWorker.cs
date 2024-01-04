// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Models.Delete;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices;

public class DeletedInstanceCleanupWorker
{
    private readonly ILogger<DeletedInstanceCleanupWorker> _logger;
    private readonly IDeleteService _deleteService;
    private readonly DeleteMeter _deleteMeter;
    private readonly TimeSpan _pollingInterval;
    private readonly int _batchSize;

    public DeletedInstanceCleanupWorker(
        IDeleteService deleteService,
        DeleteMeter deleteMeter,
        IOptions<DeletedInstanceCleanupConfiguration> backgroundCleanupConfiguration,
        ILogger<DeletedInstanceCleanupWorker> logger)
    {
        EnsureArg.IsNotNull(deleteService, nameof(deleteService));
        EnsureArg.IsNotNull(deleteMeter, nameof(deleteMeter));
        EnsureArg.IsNotNull(backgroundCleanupConfiguration?.Value, nameof(backgroundCleanupConfiguration));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _deleteService = deleteService;
        _deleteMeter = deleteMeter;
        _pollingInterval = backgroundCleanupConfiguration.Value.PollingInterval;
        _batchSize = backgroundCleanupConfiguration.Value.BatchSize;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Do not throw exceptions other than those for cancellation.")]
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_pollingInterval, stoppingToken).ConfigureAwait(false);

                // Send metrics related to deletion progress
                DeleteMetrics metrics = await _deleteService.GetMetricsAsync(stoppingToken);

                _deleteMeter.OldestRequestedDeletion.Add(metrics.OldestDeletion.ToUnixTimeSeconds());
                _deleteMeter.CountDeletionsMaxRetry.Add(metrics.TotalExhaustedRetries);

                // Delete all instances pending deletion
                bool success;
                int retrievedInstanceCount;
                do
                {
                    (success, retrievedInstanceCount) = await _deleteService.CleanupDeletedInstancesAsync(stoppingToken);
                }
                while (success && retrievedInstanceCount == _batchSize);
            }
            catch (DataStoreNotReadyException)
            {
                _logger.LogInformation("The data store is not currently ready. Processing will continue after the next wait period.");
            }
            catch (SqlException sqlEx) when (sqlEx.IsCMKError())
            {
                _logger.LogInformation(sqlEx, "The customer-managed key is misconfigured by the customer.");
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
}
