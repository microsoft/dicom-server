// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to process the list of <see cref="IDicomInstanceEntry"/>.
    /// </summary>
    public interface IStoreService
    {
        /// <summary>
        /// Asynchronously processes the <paramref name="instanceEntries"/>.
        /// </summary>
        /// <remarks>
        /// If the <paramref name="requiredStudyInstanceUid"/> is specified, every element in the
        /// <paramref name="instanceEntries"/> must have the given attribute value.
        /// </remarks>
        /// <param name="instanceEntries">The list of <see cref="IDicomInstanceEntry"/> to process.</param>
        /// <param name="requiredStudyInstanceUid">An optional value for the StudyInstanceUID tag.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="partitionId">Partition Id</param>
        /// <returns>A task that represents the asynchronous process operation.</returns>
        Task<StoreResponse> ProcessAsync(
            IReadOnlyList<IDicomInstanceEntry> instanceEntries,
            string requiredStudyInstanceUid,
            CancellationToken cancellationToken,
            string partitionId = null);
    }
}
