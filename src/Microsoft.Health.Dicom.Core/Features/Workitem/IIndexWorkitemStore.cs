// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to index UPS-RS workitems.
    /// </summary>
    public interface IIndexWorkitemStore
    {
        /// <summary>
        /// Asynchronously begin the creation of a workitem instance.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="dataset">The DICOM dataset to index.</param>
        /// <param name="queryTags">Queryable workitem tags</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that gets the workitem key.</returns>
        Task<long> BeginAddWorkitemAsync(int partitionKey, DicomDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously completes the creation of a workitem instance.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="workitemKey">The workitem instance key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous update operation.</returns>
        Task EndAddWorkitemAsync(int partitionKey, long workitemKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously begins deleting a workitem instance.
        /// </summary>
        /// <param name="workitemMetadata">The Workitem metadata.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task representing the method status.</returns>
        Task BeginUpdateWorkitemAsync(WorkitemMetadataStoreEntry workitemMetadata, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously completes the deletion of a workitem instance.
        /// </summary>
        /// <param name="workitemMetadata">The Workitem metadata.</param>
        /// <param name="dataset">The DICOM dataset to index.</param>
        /// <param name="queryTags">Queryable workitem tags</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task representing the method status.</returns>
        Task EndUpdateWorkitemAsync(WorkitemMetadataStoreEntry workitemMetadata, DicomDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously (soft) locks the workitem instance.
        /// </summary>
        /// <param name="workitemMetadata">The Workitem metadata.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task LockWorkitemAsync(WorkitemMetadataStoreEntry workitemMetadata, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously unlocks the workitem instance.
        /// </summary>
        /// <param name="workitemMetadata">The Workitem metadata.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task UnlockWorkitemAsync(WorkitemMetadataStoreEntry workitemMetadata, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deletes a workitem instance.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="workitemUid">Workitem instance UID</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task representing the method status.</returns>
        Task DeleteWorkitemAsync(int partitionKey, string workitemUid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets workitem query tags
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that gets workitem query tags.</returns>
        Task<IReadOnlyList<WorkitemQueryTagStoreEntry>> GetWorkitemQueryTagsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets workitem metadata
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="workitemUid">Workitem instance UID</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that gets workitem query tags.</returns>
        Task<WorkitemMetadataStoreEntry> GetWorkitemMetadataAsync(int partitionKey, string workitemUid, CancellationToken cancellationToken = default);
    }
}
