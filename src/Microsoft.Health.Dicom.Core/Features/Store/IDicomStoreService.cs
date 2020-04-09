// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to process and store the DICOM instance entries.
    /// </summary>
    public interface IDicomStoreService
    {
        /// <summary>
        /// Stores a DICOM instance entry.
        /// </summary>
        /// <param name="dicomInstanceEntry">The DICOM instance entry to store.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous store operation.</returns>
        Task StoreDicomInstanceEntryAsync(
            IDicomInstanceEntry dicomInstanceEntry,
            CancellationToken cancellationToken);
    }
}
