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
    /// Provides functionality to orchestrate persisting the DICOM instance entry to the data stores.
    /// </summary>
    public interface IDicomStorePersistenceOrchestrator
    {
        /// <summary>
        /// Persists the DICOM instance entry.
        /// </summary>
        /// <param name="dicomInstanceEntry">The DICOM instance entry to persist.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An instance of <see cref="Task"/>.</returns>
        Task PersistDicomInstanceEntryAsync(IDicomInstanceEntry dicomInstanceEntry, CancellationToken cancellationToken = default);
    }
}
