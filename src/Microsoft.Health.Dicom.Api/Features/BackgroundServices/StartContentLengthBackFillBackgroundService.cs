// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices;

public class StartContentLengthBackFillBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartContentLengthBackFillBackgroundService> _logger;
    private readonly ContentLengthBackFillConfiguration _config;

    public StartContentLengthBackFillBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<ContentLengthBackFillConfiguration> options,
        ILogger<StartContentLengthBackFillBackgroundService> logger)
    {
        _serviceProvider = EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(options, nameof(options));
        _config = EnsureArg.IsNotNull(options?.Value, nameof(options));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Do not throw exceptions.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();

            IDicomOperationsClient operationsClient = scope.ServiceProvider.GetRequiredService<IDicomOperationsClient>();

            // Get existing operation status
            OperationCheckpointState<DicomOperation> existingInstance = await operationsClient.GetLastCheckpointAsync(_config.OperationId, stoppingToken);

            if (existingInstance == null)
            {
                _logger.LogInformation("No existing content length backfill fixing operation.");
            }
            else
            {
                _logger.LogInformation("Existing content length backfill operation is in status: '{Status}'", existingInstance.Status);
            }

            if (IsOperationInterruptedOrNull(existingInstance))
            {
                await operationsClient.StartContentLengthBackFillOperationAsync(
                    _config.OperationId,
                    stoppingToken);
            }
            else if (existingInstance.Status == OperationStatus.Succeeded)
            {
                _logger.LogInformation("Content length backfill operation with ID '{InstanceId}' has already completed successfully.", _config.OperationId);
            }
            else
            {
                _logger.LogInformation("Content length backfill operation with ID '{InstanceId}' has already been started by another client.", _config.OperationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while starting content length backfill operation.");
        }
    }

    private static bool IsOperationInterruptedOrNull(OperationCheckpointState<DicomOperation> operation)
    {
        return operation == null || operation.Status is OperationStatus.Canceled or OperationStatus.Failed;
    }
}