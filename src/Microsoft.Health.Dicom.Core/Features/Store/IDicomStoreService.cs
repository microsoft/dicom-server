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
    public interface IDicomStoreService
    {
        /// <summary>
        /// Asynchronously processes the <paramref name="dicomInstanceEntries"/>.
        /// </summary>
        /// <param name="dicomInstanceEntries">The list of <see cref="IDicomInstanceEntry"/> to process.</param>
        /// <param name="requiredStudyInstanceUid">
        /// If supplied, the StudyInstanceUID in the <paramref name="dicomDataset"/> must match to be considered valid.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous process operation.</returns>
        Task<DicomStoreResponse> ProcessAsync(
            IReadOnlyList<IDicomInstanceEntry> dicomInstanceEntries,
            string requiredStudyInstanceUid,
            CancellationToken cancellationToken);
    }
}
