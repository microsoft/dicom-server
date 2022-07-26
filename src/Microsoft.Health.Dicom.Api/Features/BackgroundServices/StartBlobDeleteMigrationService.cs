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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Models.BlobMigration;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices;

/// <summary>
/// Background service to delete olb blob format after copy operation is successful
/// </summary>
public class StartBlobDeleteMigrationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartBlobDeleteMigrationService> _logger;
    private readonly BlobMigrationConfiguration _blobMigrationFormatConfiguration;

    public StartBlobDeleteMigrationService(
        IServiceProvider serviceProvider,
        IOptions<BlobMigrationConfiguration> blobMigrationFormatConfiguration,
        ILogger<StartBlobDeleteMigrationService> logger)
    {
        EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        EnsureArg.IsNotNull(blobMigrationFormatConfiguration, nameof(blobMigrationFormatConfiguration));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _serviceProvider = serviceProvider;
        _blobMigrationFormatConfiguration = blobMigrationFormatConfiguration.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();

            // Start the background service only when the flag is turned on and the format type is new.
            if (_blobMigrationFormatConfiguration.FormatType == BlobMigrationFormatType.New && _blobMigrationFormatConfiguration.StartDelete)
            {
                IDicomOperationsClient operationsClient = scope.ServiceProvider.GetRequiredService<IDicomOperationsClient>();

                // Get existing delete operation status
                OperationCheckpointState<DicomOperation> existingInstance = await operationsClient.GetLastCheckpointAsync(_blobMigrationFormatConfiguration.DeleteOperationId, stoppingToken); ;

                if (existingInstance == null)
                {
                    _logger.LogDebug("No existing delete operation.");
                }
                else
                {
                    _logger.LogDebug("Existing delete operation is in status: '{Status}'", existingInstance.Status);
                }

                OperationCheckpointState<DicomOperation> copyOperation = await operationsClient.GetLastCheckpointAsync(_blobMigrationFormatConfiguration.CopyOperationId, stoppingToken);

                if (IsOperationInterruptedOrNull(existingInstance))
                {
                    // Make sure copy operation is completed before starting delete operation
                    if (copyOperation?.Status == OperationStatus.Completed)
                    {
                        var checkpoint = existingInstance?.Checkpoint as BlobMigrationCheckpoint;

                        await operationsClient.StartBlobDeleteAsync(_blobMigrationFormatConfiguration.DeleteOperationId, checkpoint?.Completed, stoppingToken);
                    }
                    else
                    {
                        _logger.LogDebug("Copy operation not exists or not in completed status. '{Status}'. Failed to start delete operation.", copyOperation?.Status);
                    }
                }
                else if (existingInstance.Status == OperationStatus.Completed)
                {
                    _logger.LogInformation("Delete operation with ID '{InstanceId}' has already completed successfully.", _blobMigrationFormatConfiguration.DeleteOperationId);
                }
                else
                {
                    _logger.LogInformation("Delete operation with ID '{InstanceId}' has already been started by another client.", _blobMigrationFormatConfiguration.DeleteOperationId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled exception while starting blob delete migration.");
        }
    }

    private static bool IsOperationInterruptedOrNull(OperationCheckpointState<DicomOperation> operation)
    {
        return operation == null
            || operation.Status == OperationStatus.Canceled
            || operation.Status == OperationStatus.Failed;
    }
}
