// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to manage DICOM instance index.
    /// </summary>
    public interface IDicomIndexDataStore
    {
        /// <summary>
        /// Asynchronously creates a new instance index.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset to index.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous create operation.</returns>
        Task<long> CreateInstanceIndexAsync(DicomDataset dicomDataset, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deletes the indices of all instances which belongs to the study specified by the <paramref name="studyInstanceUid"/>.
        /// </summary>
        /// <param name="studyInstanceUid">The StudyInstanceUID.</param>
        /// <param name="cleanupAfter">The date that the record can be cleaned up.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteStudyIndexAsync(string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deletes the indices of all instances which belong to the series specified by the <paramref name="studyInstanceUid"/> and <paramref name="seriesInstanceUid"/>.
        /// </summary>
        /// <param name="studyInstanceUid">The StudyInstanceUID.</param>
        /// <param name="seriesInstanceUid">The SeriesInstanceUID.</param>
        /// <param name="cleanupAfter">The date that the record can be cleaned up.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter,  CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deletes the indices of the instance specified by the <paramref name="studyInstanceUid"/>, <paramref name="seriesInstanceUid"/>, and <paramref name="sopInstanceUid"/>.
        /// </summary>
        /// <param name="studyInstanceUid">The StudyInstanceUID.</param>
        /// <param name="seriesInstanceUid">The SeriesInstanceUID.</param>
        /// <param name="sopInstanceUid">The SopInstanceUID.</param>
        /// <param name="cleanupAfter">The date that the record can be cleaned up.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter,  CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously updates the status of an existing instance index.
        /// </summary>
        /// <param name="dicomInstanceIdentifier">The DICOM instance identifier.</param>
        /// <param name="status">The status to update to.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous update operation.</returns>
        Task UpdateInstanceIndexStatusAsync(VersionedDicomInstanceIdentifier dicomInstanceIdentifier, DicomIndexStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Return a collection of deleted instances.
        /// </summary>
        /// <param name="batchSize">The number of entries to return.</param>
        /// <param name="maxRetries">The maximum number of times a cleanup should be attempted.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of deleted instances to cleanup.</returns>
        Task<IEnumerable<VersionedDicomInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an item from the list of deleted entries that need to be cleaned up.
        /// </summary>
        /// <param name="versionedInstanceIdentifier">The DICOM instance identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous delete operation</returns>
        Task DeleteDeletedInstanceAsync(VersionedDicomInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increments the retry count of a deleted instance.
        /// </summary>
        /// <param name="versionedInstanceIdentifier">The DICOM instance identifier.</param>
        /// <param name="cleanupAfter">The date which cleanup can be attempted again</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous update operation</returns>
        Task<int> IncrementDeletedInstanceRetryAsync(VersionedDicomInstanceIdentifier versionedInstanceIdentifier, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default);
    }
}
