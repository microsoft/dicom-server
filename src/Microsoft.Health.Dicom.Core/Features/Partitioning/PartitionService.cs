// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Messages.Partitioning;

namespace Microsoft.Health.Dicom.Core.Features.Partitioning;

public class PartitionService : IPartitionService
{
    private readonly IPartitionStore _partitionStore;
    private readonly PartitionCache _partitionCache;
    private readonly ILogger<PartitionService> _logger;

    public PartitionService(PartitionCache partitionCache, IPartitionStore partitionStore, ILogger<PartitionService> logger)
    {
        _partitionStore = EnsureArg.IsNotNull(partitionStore, nameof(partitionStore));
        _partitionCache = EnsureArg.IsNotNull(partitionCache, nameof(partitionCache));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<AddPartitionResponse> AddPartitionAsync(string partitionName, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(partitionName, nameof(partitionName));

        _logger.LogInformation("Creating partition with name '{PartitionName}'.", partitionName);

        var partition = await _partitionCache.GetAsync(partitionName, partitionName, _partitionStore.AddPartitionAsync, cancellationToken);

        return new AddPartitionResponse(partition);
    }

    public async Task<GetPartitionResponse> GetPartitionAsync(string partitionName, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(partitionName, nameof(partitionName));

        _logger.LogInformation("Getting partition with name '{PartitionName}'.", partitionName);

        var partition = await _partitionCache.GetAsync(partitionName, partitionName, _partitionStore.GetPartitionAsync, cancellationToken);
        return new GetPartitionResponse(partition);
    }

    public async Task<GetPartitionsResponse> GetPartitionsAsync(CancellationToken cancellationToken = default)
    {
        var partitions = await _partitionStore.GetPartitionsAsync(cancellationToken);
        return new GetPartitionsResponse(partitions.ToList());
    }
}
