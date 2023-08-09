// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Messages.Partitioning;

namespace Microsoft.Health.Dicom.Core.Features.Partitioning;

public interface IPartitionService
{
    Task<GetOrAddPartitionResponse> GetOrAddPartitionAsync(string partitionName, CancellationToken cancellationToken = default);

    Task<GetPartitionsResponse> GetPartitionsAsync(CancellationToken cancellationToken = default);

    Task<GetPartitionResponse> GetPartitionAsync(string partitionName, CancellationToken cancellationToken = default);
}
