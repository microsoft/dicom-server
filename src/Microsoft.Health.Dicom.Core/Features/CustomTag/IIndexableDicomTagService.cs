// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Service provides indexable dicom tags.
    /// </summary>
    public interface IIndexableDicomTagService
    {
        /// <summary>
        /// Get Indexable dicom tags.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Indexable dicom tags.</returns>
        Task<IReadOnlyCollection<IndexableDicomTag>> GetIndexableDicomTagsAsync(CancellationToken cancellationToken = default);
    }
}
