// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Reindex
{
    /// <summary>
    /// The store saving extended query tags.
    /// </summary>
    public interface IReindexJobTagStore
    {
        Task<IEnumerable<ReindexJobTagStoreEntry>> GetReindexJobStoreEntryAsync(string jobId, CancellationToken cancellationToken = default);

        Task UpdateJobTagStatus(string jobId, int tagKey, ReindexJobTagStatus newStatus, CancellationToken cancellationToken = default);

        Task<ReindexJobTagStoreEntry> GetReindexJobStoreEntryAsync(int tagKey, CancellationToken cancellationToken = default);
    }
}
