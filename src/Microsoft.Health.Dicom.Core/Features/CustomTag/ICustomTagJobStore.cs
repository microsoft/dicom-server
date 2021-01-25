// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public interface ICustomTagJobStore
    {
        // only acquire Queued jobs, and job status should be updated
        Task<IEnumerable<CustomTagJob>> AcquireCustomTagJobsAsync(int maxCount, CancellationToken cancellationToken = default);

        Task<IEnumerable<CustomTagStoreEntry>> GetCustomTagsOnJobAsync(long jobKey, CancellationToken cancellationToken = default);

        Task<CustomTagJob> GetCustomTagJobAsync(long jobKey, CancellationToken cancellationToken = default);

        // Remove custom tag job from both TagJobStore and JobStore
        Task RemoveCustomTagJobAsync(long jobKey, CancellationToken cancellationToken = default);

        Task UpdateCustomTagJobCompletedWatermarkAsync(long jobKey, long? completedWatermark, CancellationToken cancellationToken = default);

        Task UpdateCustomTagJobStatusAsync(long jobKey, CustomTagJobStatus status, CancellationToken cancellationToken = default);
    }
}
