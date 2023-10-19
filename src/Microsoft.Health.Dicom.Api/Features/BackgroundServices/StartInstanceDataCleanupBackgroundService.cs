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

public class StartInstanceDataCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartInstanceDataCleanupBackgroundService> _logger;
    private readonly InstanceDataCleanupConfiguration _instanceDataCleanupConfiguration;

    public StartInstanceDataCleanupBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<InstanceDataCleanupConfiguration> options,
        ILogger<StartInstanceDataCleanupBackgroundService> logger)
    {
        _serviceProvider = EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(options, nameof(options));
        _instanceDataCleanupConfiguration = options.Value;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Do not throw exceptions.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();

            IDicomOperationsClient operationsClient = scope.ServiceProvider.GetRequiredService<IDicomOperationsClient>();

            // Get existing operation status
            OperationCheckpointState<DicomOperation> existingInstance = await operationsClient.GetLastCheckpointAsync(_instanceDataCleanupConfiguration.OperationId, stoppingToken);

            if (existingInstance == null)
            {
                _logger.LogInformation("No existing frame range fixing operation.");
            }
            else
            {
                _logger.LogInformation("Existing data cleanup operation is in status: '{Status}'", existingInstance.Status);
            }

            if (IsOperationInterruptedOrNull(existingInstance))
            {
                await operationsClient.StartInstanceDataCleanupOperationAsync(
                    _instanceDataCleanupConfiguration.OperationId,
                    _instanceDataCleanupConfiguration.StartTimeStamp,
                    _instanceDataCleanupConfiguration.EndTimeStamp,
                    stoppingToken);
            }
            else if (existingInstance.Status == OperationStatus.Succeeded)
            {
                _logger.LogInformation("Data cleanup operation with ID '{InstanceId}' has already completed successfully.", _instanceDataCleanupConfiguration.OperationId);
            }
            else
            {
                _logger.LogInformation("Data cleanup operation with ID '{InstanceId}' has already been started by another client.", _instanceDataCleanupConfiguration.OperationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled exception while starting data cleanup operation.");
        }
    }

    private static bool IsOperationInterruptedOrNull(OperationCheckpointState<DicomOperation> operation)
    {
        return operation == null
            || operation.Status == OperationStatus.Canceled
            || operation.Status == OperationStatus.Failed;
    }
}
