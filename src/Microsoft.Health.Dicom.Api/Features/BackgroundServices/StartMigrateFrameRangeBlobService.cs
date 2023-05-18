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

/// <summary>
/// Background service to migrate frame range blob with space.
/// </summary>
public class StartMigrateFrameRangeBlobService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartMigrateFrameRangeBlobService> _logger;
    private readonly FrameRangeMigrationConfiguration _framRangeBlobConfiguration;

    public StartMigrateFrameRangeBlobService(
        IServiceProvider serviceProvider,
        IOptions<FrameRangeMigrationConfiguration> framRangeBlobConfiguration,
        ILogger<StartMigrateFrameRangeBlobService> logger)
    {
        _serviceProvider = EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(framRangeBlobConfiguration, nameof(framRangeBlobConfiguration));
        _framRangeBlobConfiguration = framRangeBlobConfiguration.Value;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Do not throw exceptions.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();

            IDicomOperationsClient operationsClient = scope.ServiceProvider.GetRequiredService<IDicomOperationsClient>();

            // Get existing operation status
            OperationCheckpointState<DicomOperation> existingInstance = await operationsClient.GetLastCheckpointAsync(_framRangeBlobConfiguration.OperationId, stoppingToken);

            if (existingInstance == null)
            {
                _logger.LogInformation("No existing migration operation.");
            }
            else
            {
                _logger.LogInformation("Existing migration is in status: '{Status}'", existingInstance.Status);
            }

            if (IsOperationInterruptedOrNull(existingInstance))
            {
                await operationsClient.StartMigratingFrameRangeBlobAsync(
                    _framRangeBlobConfiguration.OperationId,
                    _framRangeBlobConfiguration.StartTimeStamp,
                    _framRangeBlobConfiguration.EndTimeStamp,
                    stoppingToken);
            }
            else if (existingInstance.Status == OperationStatus.Succeeded)
            {
                _logger.LogInformation("Migrating operation with ID '{InstanceId}' has already completed successfully.", _framRangeBlobConfiguration.OperationId);
            }
            else
            {
                _logger.LogInformation("Migrating operation with ID '{InstanceId}' has already been started by another client.", _framRangeBlobConfiguration.OperationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled exception while starting migration operation.");
        }
    }

    private static bool IsOperationInterruptedOrNull(OperationCheckpointState<DicomOperation> operation)
    {
        return operation == null
            || operation.Status == OperationStatus.Canceled
            || operation.Status == OperationStatus.Failed;
    }
}
