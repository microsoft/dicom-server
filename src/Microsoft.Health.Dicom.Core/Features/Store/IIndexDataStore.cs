// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to manage DICOM instance index.
    /// </summary>
    public interface IIndexDataStore
    {
        /// <summary>
        /// Asynchronously creates a new instance index.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset to index.</param>
        /// <param name="queryTags">Queryable dicom tags</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous create operation.</returns>
        Task<long> CreateInstanceIndexAsync(DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default);

        Task ReindexInstanceAsync(DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default);

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
        Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deletes the indices of the instance specified by the <paramref name="studyInstanceUid"/>, <paramref name="seriesInstanceUid"/>, and <paramref name="sopInstanceUid"/>.
        /// </summary>
        /// <param name="studyInstanceUid">The StudyInstanceUID.</param>
        /// <param name="seriesInstanceUid">The SeriesInstanceUID.</param>
        /// <param name="sopInstanceUid">The SopInstanceUID.</param>
        /// <param name="cleanupAfter">The date that the record can be cleaned up.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously updates the status of an existing instance index.
        /// </summary>
        /// <param name="versionedInstanceIdentifier">The DICOM instance identifier.</param>
        /// <param name="status">The status to update to.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous update operation.</returns>
        Task UpdateInstanceIndexStatusAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, IndexStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Return a collection of deleted instances.
        /// </summary>
        /// <param name="batchSize">The number of entries to return.</param>
        /// <param name="maxRetries">The maximum number of times a cleanup should be attempted.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of deleted instances to cleanup.</returns>
        Task<IEnumerable<VersionedInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an item from the list of deleted entries that need to be cleaned up.
        /// </summary>
        /// <param name="versionedInstanceIdentifier">The DICOM instance identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous delete operation</returns>
        Task DeleteDeletedInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increments the retry count of a deleted instance.
        /// </summary>
        /// <param name="versionedInstanceIdentifier">The DICOM instance identifier.</param>
        /// <param name="cleanupAfter">The date which cleanup can be attempted again</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous update operation</returns>
        Task<int> IncrementDeletedInstanceRetryAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the number of deleted instances which have reached the max number of retries.
        /// </summary>
        /// <param name="maxNumberOfRetries">The max number of retries.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that gets the count</returns>
        Task<int> RetrieveNumExhaustedDeletedInstanceAttemptsAsync(int maxNumberOfRetries, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the <see cref="DateTimeOffset"/> of oldest instance waiting to be delated
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that gets the date of the oldest deleted instance</returns>
        Task<DateTimeOffset> GetOldestDeletedAsync(CancellationToken cancellationToken = default);
    }
}
