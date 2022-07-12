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

public class StartBlobDeleteMigrationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BlobMigrationFormatType _blobMigrationFormatType;
    private readonly bool _startBlobDelete;
    private readonly ILogger<StartBlobMigrationService> _logger;
    private readonly Guid _deleteOperationId;
    private readonly Guid _copyOperationId;

    public StartBlobDeleteMigrationService(
        IServiceProvider serviceProvider,
        IOptions<BlobMigrationConfiguration> blobMigrationFormatConfiguration,
        ILogger<StartBlobMigrationService> logger)
    {
        EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        EnsureArg.IsNotNull(blobMigrationFormatConfiguration, nameof(blobMigrationFormatConfiguration));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _serviceProvider = serviceProvider;
        _blobMigrationFormatType = blobMigrationFormatConfiguration.Value.FormatType;
        _startBlobDelete = blobMigrationFormatConfiguration.Value.StartCopy;
        _deleteOperationId = blobMigrationFormatConfiguration.Value.DeleteOperationId;
        _copyOperationId = blobMigrationFormatConfiguration.Value.CopyOperationId;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                // Start the background service only when the flag is turned on and the format type is not new service.
                if (_blobMigrationFormatType == BlobMigrationFormatType.New && _startBlobDelete)
                {
                    IDicomOperationsClient operationsClient = scope.ServiceProvider.GetRequiredService<IDicomOperationsClient>();

                    OperationCheckpointState<DicomOperation> existingInstance = await operationsClient.GetLastCheckpointAsync(_deleteOperationId, stoppingToken);

                    if (existingInstance == null)
                    {
                        _logger.LogDebug("No existing delete operation.");
                    }
                    else
                    {
                        _logger.LogDebug("Existing delete operation is in status: '{Status}'", existingInstance.Status);
                    }

                    OperationCheckpointState<DicomOperation> copyOperation = await operationsClient.GetLastCheckpointAsync(_copyOperationId, stoppingToken);

                    // Make sure delete operation is completed before starting delete operation
                    if (IsOperationInterruptedOrNull(existingInstance) && copyOperation?.Status == OperationStatus.Completed)
                    {
                        var checkpoint = existingInstance?.Checkpoint as BlobMigrationCheckpoint;

                        await operationsClient.StartBlobCopyAsync(_deleteOperationId, checkpoint?.Completed, stoppingToken);
                    }
                    else if (existingInstance.Status == OperationStatus.Completed)
                    {
                        _logger.LogInformation("Delete operation with ID '{InstanceId}' has already completed successfully.", _deleteOperationId);
                    }
                    else
                    {
                        _logger.LogInformation("Delete operation with ID '{InstanceId}' has already been started by another client.", _deleteOperationId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled exception while starting blob migration.");
        }
    }

    private static bool IsOperationInterruptedOrNull(OperationCheckpointState<DicomOperation> operation)
    {
        return operation == null
            || operation.Status == OperationStatus.Canceled
            || operation.Status == OperationStatus.Failed;
    }
}
