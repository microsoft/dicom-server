// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    /// <summary>
    /// Provides functionalities managing the DICOM instance work-item.
    /// </summary>
    public interface IWorkItemStore
    {
        /// <summary>
        /// Asynchronously stores a DICOM instance work-item.
        /// </summary>
        /// <param name="workItem">The DICOM work-item instance.</param>
        /// <param name="version">The version.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous add operation.</returns>
        Task StoreInstanceWorkItemAsync(
            WorkItem workItem,
            long version,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a DICOM instance work-item.
        /// </summary>
        /// <param name="versionedInstanceIdentifier">The DICOM instance identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get operation.</returns>
        Task<WorkItem> GetInstanceWorkItemAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deletes a DICOM instance work-item.
        /// </summary>
        /// <param name="versionedInstanceIdentifier">The DICOM instance identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteInstanceWorkItemIfExistsAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            CancellationToken cancellationToken = default);
    }
}
