// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Store.Upload;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to orchestrate persisting the DICOM instance to the data stores.
    /// </summary>
    public interface IDicomStorePersistenceOrchestrator
    {
        /// <summary>
        /// Persists the uploaded DICOM instance.
        /// </summary>
        /// <param name="uploadedDicomInstance">The DICOM instance to store.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An instance of <see cref="Task"/>.</returns>
        Task PersistUploadedDicomInstanceAsync(IUploadedDicomInstance uploadedDicomInstance, CancellationToken cancellationToken = default);
    }
}
