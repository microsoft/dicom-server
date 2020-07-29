// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.BulkImport
{
    public interface IBulkImportDataStore
    {
        Task EnableBulkImportSourceAsync(string accountName, CancellationToken cancellationToken = default);

        Task UpdateBulkImportSourceAsync(string accountName, BulkImportSourceStatus status, CancellationToken cancellationToken = default);

        Task QueueBulkImportEntriesAsync(string accountName, IReadOnlyList<BlobReference> blobReferences, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BulkImportEntry>> RetrieveBulkImportEntriesAsync(CancellationToken cancellationToken = default);

        Task UpdateBulkImportEntriesAsync(IEnumerable<(long Sequence, BulkImportEntryStatus Status)> entries, CancellationToken cancellationToken = default);
    }
}
