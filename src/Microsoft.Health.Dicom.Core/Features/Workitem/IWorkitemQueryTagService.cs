// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Service provides queryable dicom tags.
    /// </summary>
    public interface IWorkitemQueryTagService
    {
        /// <summary>
        /// Get queryable dicom tags.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Queryable dicom tags.</returns>
        Task<IReadOnlyCollection<QueryTag>> GetQueryTagsAsync(CancellationToken cancellationToken = default);
    }
}
