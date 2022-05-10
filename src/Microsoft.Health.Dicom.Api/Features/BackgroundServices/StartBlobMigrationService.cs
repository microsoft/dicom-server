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

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices;

public class StartBlobMigrationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BlobMigrationFormatType _blobMigrationFormatType;
    private readonly bool _startBlobCopy;
    private readonly ILogger<StartBlobMigrationService> _logger;

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
                    var operationsClient = scope.ServiceProvider.GetRequiredService<IDicomOperationsClient>();

                    // We also need to ensure if the operation client already not completed
                    if (operationsClient != null && !await operationsClient.IsBlobCopyCompletedAsync(stoppingToken))
                    {
                        await operationsClient.StartBlobCopyAsync(stoppingToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled exception while starting blob migration.");
        }
    }
}
