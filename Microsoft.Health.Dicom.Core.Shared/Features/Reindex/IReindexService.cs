// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Reindex
{
    public interface IReindexService
    {
        Task<string> ReindexAsync(IEnumerable<string> extendedQueryTags, CancellationToken cancellationToken = default);

        Task<ReindexJob> GetReindexJobStatusAsync(string jobId, CancellationToken cancellationToken = default);

        Task CancelReindexJobAsync(string jobId, CancellationToken cancellation = default);

        Task<ReindexJobReport> GetReindexJobReportAsync(string jobId, CancellationToken cancellationToken = default);
    }
}
