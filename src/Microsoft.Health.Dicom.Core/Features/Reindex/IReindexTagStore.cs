// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Reindex
{
    public interface IReindexTagStore
    {
        Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetTagsOnOperationAsync(
            long operationKey,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<long>> GetWatermarksAsync(long operationKey, int topN, CancellationToken cancellationToken = default);

        Task UpdateMaxWatermarkAsync(long operationKey, long maxWatarmark, CancellationToken cancellationToken = default);

        Task CompleteReindexAsync(long operationKey, CancellationToken cancellationToken = default);

    }
}
