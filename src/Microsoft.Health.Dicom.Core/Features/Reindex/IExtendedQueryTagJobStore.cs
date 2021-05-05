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
    public interface IExtendedQueryTagJobStore

    {
        Task<IEnumerable<ExtendedQueryJobTagStoreEntry>> GetExtendedQueryTagJobStoreEntryAsync(string jobId, CancellationToken cancellationToken = default);

        Task<ExtendedQueryJobTagStoreEntry> GetExtendedQueryTagJobStoreEntryAsync(int tagKey, CancellationToken cancellationToken = default);

        Task UpdateExtendedQueryTagJobStatus(string jobId, int tagKey, ExtendedQueryTagJobStatus newStatus, CancellationToken cancellationToken = default);

    }
}
