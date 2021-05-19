// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    public interface IExtendedQueryTagReindexOperationStore

    {
        Task<IReadOnlyList<ExtendedQueryTagReindexOperationEntry>> GetEntriesAsync(
            string operationId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<long>> GetNextWatermarks(string operationId, int count, CancellationToken cancellationToken = default);

        Task UpdateEndWatermarkAsync(string operationId, long endWatermark, CancellationToken cancellationToken = default);

        Task CompleteReindexOperationAsync(string operationId, CancellationToken cancellationToken = default);

    }
}
