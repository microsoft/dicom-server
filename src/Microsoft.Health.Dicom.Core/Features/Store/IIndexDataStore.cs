// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Core.Features.Store;

/// <summary>
/// Provides functionality to manage DICOM instance index.
/// </summary>
public interface IIndexDataStore
{
    /// <summary>
    /// Asynchronously begins the addition of a DICOM instance.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="dicomDataset">The DICOM dataset to index.</param>
    /// <param name="queryTags">Queryable dicom tags</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    Task<long> BeginCreateInstanceIndexAsync(Partition partition, DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously reindex a DICOM instance.
    /// </summary>
    /// <param name="dicomDataset">The DICOM dataset to reindex.</param>
    /// <param name="watermark">The DICOM instance watermark.</param>
    /// <param name="queryTags">Queryable dicom tags</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous reindex operation.</returns>
    Task ReindexInstanceAsync(DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes the indices of all instances which belongs to the study specified by the <paramref name="partition"/>, <paramref name="studyInstanceUid"/>.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="studyInstanceUid">The StudyInstanceUID.</param>
    /// <param name="cleanupAfter">The date that the record can be cleaned up.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task<IReadOnlyCollection<VersionedInstanceIdentifier>> DeleteStudyIndexAsync(Partition partition, string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes the indices of all instances which belong to the series specified by the <paramref name="partition"/>, <paramref name="studyInstanceUid"/> and <paramref name="seriesInstanceUid"/>.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="studyInstanceUid">The StudyInstanceUID.</param>
    /// <param name="seriesInstanceUid">The SeriesInstanceUID.</param>
    /// <param name="cleanupAfter">The date that the record can be cleaned up.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task<IReadOnlyCollection<VersionedInstanceIdentifier>> DeleteSeriesIndexAsync(Partition partition, string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes the indices of the instance specified by the <paramref name="partition"/>, <paramref name="studyInstanceUid"/>, <paramref name="seriesInstanceUid"/>, and <paramref name="sopInstanceUid"/>.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="studyInstanceUid">The StudyInstanceUID.</param>
    /// <param name="seriesInstanceUid">The SeriesInstanceUID.</param>
    /// <param name="sopInstanceUid">The SopInstanceUID.</param>
    /// <param name="cleanupAfter">The date that the record can be cleaned up.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task<IReadOnlyCollection<VersionedInstanceIdentifier>> DeleteInstanceIndexAsync(Partition partition, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously completes the addition of a DICOM instance.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="dicomDataset">The DICOM dataset whose status should be updated.</param>
    /// <param name="watermark">The DICOM instance watermark</param>
    /// <param name="queryTags">Queryable dicom tags</param>
    /// <param name="fileProperties">file properties</param>
    /// <param name="allowExpiredTags">Optionally allow an out-of-date snapshot of <paramref name="queryTags"/>.</param>
    /// <param name="hasFrameMetadata">Has additional frame range metadata stores.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task EndCreateInstanceIndexAsync(int partitionKey, DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, FileProperties fileProperties = null, bool allowExpiredTags = false, bool hasFrameMetadata = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return a collection of deleted instances.
    /// </summary>
    /// <param name="batchSize">The number of entries to return.</param>
    /// <param name="maxRetries">The maximum number of times a cleanup should be attempted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of deleted instances to cleanup.</returns>
    Task<IEnumerable<VersionedInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return a collection of deleted instances with properties.
    /// </summary>
    /// <param name="batchSize">The number of entries to return.</param>
    /// <param name="maxRetries">The maximum number of times a cleanup should be attempted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of deleted instances to cleanup.</returns>
    Task<IReadOnlyList<InstanceMetadata>> RetrieveDeletedInstancesWithPropertiesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default);

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
    /// Retrieves the <see cref="DateTimeOffset"/> of oldest instance waiting to be deleted
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that gets the date of the oldest deleted instance</returns>
    Task<DateTimeOffset> GetOldestDeletedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously updates a DICOM instance NewWatermark
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="versions">List of instances watermark to update</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that with list of instance metadata with new watermark.</returns>
    Task<IEnumerable<InstanceMetadata>> BeginUpdateInstanceAsync(Partition partition, IReadOnlyCollection<long> versions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously updates a DICOM instance NewWatermark
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="studyInstanceUid">StudyInstanceUID to update</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that with list of instance metadata with new watermark.</returns>
    Task<IReadOnlyList<InstanceMetadata>> BeginUpdateInstancesAsync(Partition partition, string studyInstanceUid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously bulk update all instances in a study, and update extendedquerytag with new watermark.
    /// Also creates new changefeed entry
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="studyInstanceUid"></param>
    /// <param name="dicomDataset">The DICOM dataset to index.</param>
    /// <param name="instanceMetadataList">A list of instance metadata to use to update file properties for newly stored blob file from "update"</param>
    /// <param name="queryTags">Queryable dicom tags</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    Task EndUpdateInstanceAsync(int partitionKey, string studyInstanceUid, DicomDataset dicomDataset, IReadOnlyList<InstanceMetadata> instanceMetadataList, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously updates DICOM instance HasFrameMetadata to 1
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="versions">List of instances watermark to update</param>
    /// <param name="hasFrameMetadata">Has additional frame range metadata stores.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that with list of instance metadata with new watermark.</returns>
    Task UpdateFrameDataAsync(int partitionKey, IEnumerable<long> versions, bool hasFrameMetadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously updates DICOM instance file properties content length
    /// </summary>
    /// <param name="filePropertiesByWatermark">file properties that need to get the content length updated</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that with list of instance metadata with new watermark.</returns>
    Task UpdateFilePropertiesContentLengthAsync(IReadOnlyDictionary<long, FileProperties> filePropertiesByWatermark, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves total count in FileProperty table and summation of all content length values across FileProperty table.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that gets the count</returns>
    Task<IndexedFileProperties> GetIndexedFileMetricsAsync(CancellationToken cancellationToken = default);
}
