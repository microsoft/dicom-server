// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    public interface IReindexTagStore
    {
        Task<IReadOnlyList<ReindexTagStoreEntry>> GetTagsOnOperationAsync(
            long operationKey,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<long>> GetWatermarksAsync(long operationKey, int topN, CancellationToken cancellationToken = default);

        Task UpdateMaxWatermarkAsync(string operationId, long maxWatarmark, CancellationToken cancellationToken = default);

        Task CompleteReindexAsync(string operationId, CancellationToken cancellationToken = default);

    }
}
