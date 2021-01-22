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
        // only acquire Queued jobs
        Task<IEnumerable<CustomTagJob>> AcquireCustomTagJobsAsync(int maxCount, CancellationToken cancellationToken = default);
    }
}
