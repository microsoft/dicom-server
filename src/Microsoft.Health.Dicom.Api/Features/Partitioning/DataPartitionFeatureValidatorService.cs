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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Api.Features.Partitioning;

/// <summary>
/// This hosted service performs the check at startup to ensure user cannot disable DataPartitions feature flag
/// if they already created partitioned data other than the default partition.
/// If a user created partitioned data other than the default partition, then an exception is thrown to block startup.
/// We will allow users to enable DataPartitions feature even if they have already created data,
/// it will be accessible by specifying Microsoft.Default partition name in the request.
/// </summary>
public class DataPartitionFeatureValidatorService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _isPartitionEnabled;
    private readonly ILogger<DataPartitionFeatureValidatorService> _logger;

    public DataPartitionFeatureValidatorService(
        IServiceProvider serviceProvider,
        IOptions<FeatureConfiguration> featureConfiguration,
        ILogger<DataPartitionFeatureValidatorService> logger)
    {
        _serviceProvider = serviceProvider;
        EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));

        _logger = EnsureArg.IsNotNull(logger, nameof(logger));

        _isPartitionEnabled = featureConfiguration.Value.EnableDataPartitions;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_isPartitionEnabled)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var partitionService = scope.ServiceProvider.GetRequiredService<IPartitionService>();

                    var partitions = await partitionService.GetPartitionsAsync(cancellationToken);

                    if (partitions.Entries.Count > 1)
                    {
                        throw new DataPartitionsFeatureCannotBeDisabledException();
                    }
                }
            }
            catch (DataStoreNotReadyException ex)
            {
                // If a consumer doesn't upgrade the schema, then the service won't be started. So silently failing.
                _logger.LogWarning("Silently failing, schema version not upgraded. {Message}", ex.Message);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
