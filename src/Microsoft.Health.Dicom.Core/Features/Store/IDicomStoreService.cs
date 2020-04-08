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
    /// Provides functionality to process and store the DICOM instance entries.
    /// </summary>
    public interface IDicomStoreService
    {
        /// <summary>
        /// Processes the DICOM instance entries.
        /// </summary>
        /// <param name="studyInstanceUid">If not <c>null</c>, then any instances that does not have matching StudyInstanceUid will be rejected.</param>
        /// <param name="dicomInstanceEntries">The DICOM instance entries to process.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An instance of <see cref="DicomStoreResponse"/>.</returns>
        Task<DicomStoreResponse> ProcessDicomInstanceEntriesAsync(
            string studyInstanceUid,
            IReadOnlyCollection<IDicomInstanceEntry> dicomInstanceEntries,
            CancellationToken cancellationToken);
    }
}
