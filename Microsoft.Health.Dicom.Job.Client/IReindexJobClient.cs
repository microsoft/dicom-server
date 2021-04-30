// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Jobs
{
    public interface IReindexJobClient
    {
        Task<string> CreateJobAsync(IEnumerable<string> extendedQueryTags, CancellationToken cancellationToken = default);

        Task<ReindexJobStatus> GetJobStateAsync(string jobId, CancellationToken cancellationToken = default);

        Task CancelJobAsync(string jobId, CancellationToken cancellationToken = default);
    }
}
