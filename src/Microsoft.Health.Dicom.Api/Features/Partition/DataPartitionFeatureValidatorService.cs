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
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Partition;

namespace Microsoft.Health.Dicom.Api.Features.Partition
{
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

        public DataPartitionFeatureValidatorService(
            IServiceProvider serviceProvider,
            IOptions<FeatureConfiguration> featureConfiguration)
        {
            _serviceProvider = serviceProvider;
            EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));
            _isPartitionEnabled = featureConfiguration.Value.EnableDataPartitions;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_isPartitionEnabled)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var partitionService = scope.ServiceProvider.GetRequiredService<IPartitionService>();

                    var partitionEntries = await partitionService.GetPartitionsAsync(cancellationToken);

                    if (partitionEntries.Entries.Count > 1)
                    {
                        throw new DataPartitionsFeatureCannotBeDisabledException();
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
