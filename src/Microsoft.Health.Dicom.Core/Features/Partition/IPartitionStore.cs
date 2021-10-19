// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Partition
{
    public interface IPartitionStore
    {
        Task<PartitionEntry> AddPartition(string partitionName, CancellationToken cancellationToken = default);

        Task<IEnumerable<PartitionEntry>> GetPartitions(CancellationToken cancellationToken = default);

        Task<PartitionEntry> GetPartition(string partitionName, CancellationToken cancellationToken = default);
    }
}
