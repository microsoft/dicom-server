// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Service provides queryable dicom tags.
    /// </summary>
    public interface IQueryTagService
    {
        /// <summary>
        /// Get queryable dicom tags.
        /// </summary>
        /// <param name="forceRefresh">Optionally indicate that the data should be refreshed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Queryable dicom tags.</returns>
        Task<IReadOnlyCollection<QueryTag>> GetQueryTagsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    }
}
