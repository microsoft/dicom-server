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
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices;

public class StartBlobMigrationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BlobMigrationFormatType _blobMigrationFormatType;
    private readonly bool _startBlobCopy;
    private readonly ILogger<StartBlobMigrationService> _logger;
    private readonly Guid _operationId;

    public StartBlobMigrationService(
        IServiceProvider serviceProvider,
        IOptions<BlobMigrationConfiguration> blobMigrationFormatConfiguration,
        ILogger<StartBlobMigrationService> logger)
    {
        EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        EnsureArg.IsNotNull(blobMigrationFormatConfiguration, nameof(blobMigrationFormatConfiguration));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _serviceProvider = serviceProvider;
        _blobMigrationFormatType = blobMigrationFormatConfiguration.Value.FormatType;
        _startBlobCopy = blobMigrationFormatConfiguration.Value.StartCopy;
        _operationId = blobMigrationFormatConfiguration.Value.OperationId;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                // Start the background service only when the flag is turned on and the format type is not new service.
                if (_blobMigrationFormatType != BlobMigrationFormatType.New && _startBlobCopy)
                {
                    IDicomOperationsClient operationsClient = scope.ServiceProvider.GetRequiredService<IDicomOperationsClient>();

                    OperationState<DicomOperation> existingInstance = await operationsClient.GetStateAsync(_operationId, stoppingToken);

                    if (existingInstance == null)
                    {
                        _logger.LogDebug("No existing copy operation.");
                    }
                    else
                    {
                        _logger.LogDebug("Existing copy operation is in status: '{Status}'", existingInstance.Status);
                    }

                    if (IsOperationInterruptedOrNull(existingInstance))
                    {
                        await operationsClient.StartBlobCopyAsync(_operationId, existingInstance != null, stoppingToken);
                    }
                    else if (existingInstance.Status == OperationStatus.Completed)
                    {
                        _logger.LogInformation("Copy operation with ID '{InstanceId}' has already completed successfully.", _operationId);
                    }
                    else
                    {
                        _logger.LogInformation("Copy operation with ID '{InstanceId}' has already been started by another client.", _operationId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled exception while starting blob migration.");
        }
    }

    private static bool IsOperationInterruptedOrNull(OperationState<DicomOperation> operation)
    {
        return operation == null
            || operation.Status == OperationStatus.Canceled
            || operation.Status == OperationStatus.Failed;
    }
}
