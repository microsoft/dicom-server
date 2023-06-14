// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

public interface IInstanceStore
{
    /// <summary>
    /// Gets identifiers of instances in a study.
    /// </summary>
    /// <param name="partitionEntry">The partition.</param>
    /// <param name="studyInstanceUid">The study identifier.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>Instance identifiers.</returns>
    Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifiersInStudyAsync(
        PartitionEntry partitionEntry,
        string studyInstanceUid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets identifiers of instances in a series.
    /// </summary>
    /// <param name="partitionEntry">The partition.</param>
    /// <param name="studyInstanceUid">The study identifier.</param>
    /// <param name="seriesInstanceUid">The series identifier.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>Instance identifiers.</returns>
    Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifiersInSeriesAsync(
        PartitionEntry partitionEntry,
        string studyInstanceUid,
        string seriesInstanceUid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets identifiers of instances in an instance.
    /// </summary>
    /// <param name="partitionEntry">The partition.</param>
    /// <param name="studyInstanceUid">The study identifier.</param>
    /// <param name="seriesInstanceUid">The series identifier.</param>
    /// <param name="sopInstanceUid">The instance identifier.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>Instance identifiers.</returns>
    Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifierAsync(
        PartitionEntry partitionEntry,
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets identifiers of instances within the given range of watermarks.
    /// </summary>
    /// <param name="watermarkRange">The watermark range</param>
    /// <param name="indexStatus">The index status</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The instanceidentifiers</returns>
    Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifiersByWatermarkRangeAsync(
        WatermarkRange watermarkRange,
        IndexStatus indexStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the specified number of instance batches.
    /// </summary>
    /// <param name="batchSize">The desired size of each batch.</param>
    /// <param name="batchCount">The maximum number of batches.</param>
    /// <param name="indexStatus">The index status</param>
    /// <param name="maxWatermark">An optional maximum watermark to consider.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous get operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains a list of batches as defined by their smallest and largest watermark.
    /// The size of the collection is at most the value of the <paramref name="batchCount"/> parameter.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="batchSize"/> or <paramref name="batchCount"/> is less than <c>1</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<IReadOnlyList<WatermarkRange>> GetInstanceBatchesAsync(
        int batchSize,
        int batchCount,
        IndexStatus indexStatus,
        long? maxWatermark = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets identifiers of instances with additional properties.
    /// </summary>
    /// <param name="partitionEntry">The partition.</param>
    /// <param name="studyInstanceUid">The study identifier.</param>
    /// <param name="seriesInstanceUid">The series identifier.</param>
    /// <param name="sopInstanceUid">The instance identifier.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>Instance identifiers.</returns>
    Task<IReadOnlyList<InstanceMetadata>> GetInstanceIdentifierWithPropertiesAsync(
        PartitionEntry partitionEntry,
        string studyInstanceUid,
        string seriesInstanceUid = null,
        string sopInstanceUid = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the specified number of instance batches filtered by timestamp.
    /// </summary>
    /// <param name="batchSize">The desired size of each batch.</param>
    /// <param name="batchCount">The maximum number of batches.</param>
    /// <param name="indexStatus">The index status</param>
    /// <param name="startTimeStamp">Start filterstamp</param>
    /// <param name="endTimeStamp">End filterstamp</param>
    /// <param name="maxWatermark">An optional maximum watermark to consider.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous get operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains a list of batches as defined by their smallest and largest watermark.
    /// The size of the collection is at most the value of the <paramref name="batchCount"/> parameter.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="batchSize"/> or <paramref name="batchCount"/> is less than <c>1</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<IReadOnlyList<WatermarkRange>> GetInstanceBatchesByTimeStampAsync(
        int batchSize,
        int batchCount,
        IndexStatus indexStatus,
        DateTimeOffset startTimeStamp,
        DateTimeOffset endTimeStamp,
        long? maxWatermark = null,
        CancellationToken cancellationToken = default);

}
