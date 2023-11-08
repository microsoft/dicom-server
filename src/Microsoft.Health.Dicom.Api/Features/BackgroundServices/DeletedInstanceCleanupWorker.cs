// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Configs;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Models.Delete;
using Polly;
using Polly.Retry;

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices;

public class DeletedInstanceCleanupWorker
{
    private readonly ILogger<DeletedInstanceCleanupWorker> _logger;
    private readonly DeleteMeter _deleteMeter;
    private readonly DeletedInstanceCleanupConfiguration _options;
    private readonly IDeleteService _deleteService;
    private readonly ResiliencePipeline _pipeline;

    public DeletedInstanceCleanupWorker(
        IDeleteService deleteService,
        DeleteMeter deleteMeter,
        IOptions<DeleteWorkerOptions> workerOptions,
        IOptions<DeletedInstanceCleanupConfiguration> deleteOptions,
        ILogger<DeletedInstanceCleanupWorker> logger)
    {
        _deleteService = EnsureArg.IsNotNull(deleteService, nameof(deleteService));
        _deleteMeter = EnsureArg.IsNotNull(deleteMeter, nameof(deleteMeter));
        _options = EnsureArg.IsNotNull(deleteOptions?.Value, nameof(deleteOptions));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));

        DeleteWorkerOptions worker = EnsureArg.IsNotNull(workerOptions?.Value, nameof(workerOptions));
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                BackoffType = worker.BackoffType,
                MaxDelay = worker.MaxDelay,
                MaxRetryAttempts = worker.MaxRetryAttempts,
                Delay = worker.Delay,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Failed to clean up deleted instances on attempt #{Attempt}. Retrying in {Delay}.",
                        args.AttemptNumber,
                        args.RetryDelay);

                    return default;
                },
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not OperationCanceledException),
                UseJitter = worker.UseJitter,
            })
            .Build();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Do not throw exceptions other than those for cancellation.")]
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_options.PollingInterval, stoppingToken);
            await _pipeline.ExecuteAsync(CleanUpDeletedInstancesAsync, stoppingToken);
        }
    }

    private async ValueTask CleanUpDeletedInstancesAsync(CancellationToken cancellationToken)
    {
        DeleteSummary summary;

        do
        {
            try
            {
                DeleteMetrics metrics = await _deleteService.GetMetricsAsync(cancellationToken);
                _deleteMeter.OldestRequestedDeletion.Add(metrics.OldestDeletion.ToUnixTimeSeconds());
                _deleteMeter.CountDeletionsMaxRetry.Add(metrics.TotalExhaustedRetries);

                summary = await _deleteService.CleanUpDeletedInstancesAsync(cancellationToken);

                if (summary.Found > 0)
                    _logger.LogInformation("Successfully deleted {Deleted}/{Found}", summary.Deleted, summary.Found);
            }
            catch (DataStoreNotReadyException)
            {
                _logger.LogInformation("The data store is not currently ready. Processing will continue after the next wait period.");
                summary = default;
            }
        }
        while (summary.Found == summary.Deleted && summary.Found == _options.BatchSize);
    }
}
