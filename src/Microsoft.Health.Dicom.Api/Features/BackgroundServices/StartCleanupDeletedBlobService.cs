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
using Microsoft.Health.Dicom.Functions.Migration;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices;

/// <summary>
/// Background service to delete dangling olb blob format files
/// </summary>
public class StartCleanupDeletedBlobService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartBlobDeleteMigrationService> _logger;
    private readonly BlobMigrationConfiguration _blobMigrationFormatConfiguration;

    public StartCleanupDeletedBlobService(
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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Do not throw exceptions.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();

            IDicomOperationsClient operationsClient = scope.ServiceProvider.GetRequiredService<IDicomOperationsClient>();

            // Get existing delete operation status
            OperationCheckpointState<DicomOperation> existingInstance = await operationsClient.GetLastCheckpointAsync(_blobMigrationFormatConfiguration.CleanupDeletedOperationId, stoppingToken);

            if (existingInstance == null)
            {
                _logger.LogDebug("No existing cleanup deleted operation.");
            }
            else
            {
                _logger.LogDebug("Existing clean up deleted operation is in status: '{Status}'", existingInstance.Status);
            }

            if (IsOperationInterruptedOrNull(existingInstance))
            {
                var checkpoint = existingInstance?.Checkpoint as BlobMigrationCheckpoint;

                await operationsClient.StartBlobCleanupDeletedAsync(_blobMigrationFormatConfiguration.CleanupDeletedOperationId, _blobMigrationFormatConfiguration.CleanupFilterTimeStamp, checkpoint?.Completed, stoppingToken);
            }
            else if (existingInstance.Status == OperationStatus.Succeeded)
            {
                _logger.LogInformation("Cleanup delete operation with ID '{InstanceId}' has already completed successfully.", _blobMigrationFormatConfiguration.CleanupDeletedOperationId);
            }
            else
            {
                _logger.LogInformation("Cleanup delete operation with ID '{InstanceId}' has already been started by another client.", _blobMigrationFormatConfiguration.CleanupDeletedOperationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled exception while starting blob cleanup deleted operation.");
        }
    }

    private static bool IsOperationInterruptedOrNull(OperationCheckpointState<DicomOperation> operation)
    {
        return operation == null
            || operation.Status == OperationStatus.Canceled
            || operation.Status == OperationStatus.Failed;
    }
}
