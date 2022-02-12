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
using Microsoft.Health.Dicom.Core.Features.Query.Model;

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
        /// <returns>A task that gets the workitem watermark.</returns>
        Task<long> BeginAddWorkitemAsync(int partitionKey, DicomDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously begin the creation of a workitem instance.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="dataset">The DICOM dataset to index.</param>
        /// <param name="queryTags">Queryable workitem tags</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that adds the workitem and gets the workitemkey and watermark.</returns>
        Task<(long WorkitemKey, long Watermark)?> BeginAddWorkitemWithWatermarkAsync(int partitionKey, DicomDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously completes the creation of a workitem instance.
        /// </summary>
        /// <param name="partitionKey">The Partition Key</param>
        /// <param name="workitemKey">The workitem instance key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous update operation.</returns>
        Task EndAddWorkitemAsync(int partitionKey, long workitemKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously updates the workitem instance status.
        /// </summary>
        /// <param name="partitionKey">The Partition Key</param>
        /// <param name="workitemKey">The workitem instance key.</param>
        /// <param name="status">The Workitem status</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task UpdateWorkitemStatusAsync(int partitionKey, long workitemKey, WorkitemStoreStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously updates the workitem instance's Procedure Step State.
        /// </summary>
        /// <param name="workitemMetadata">The Workitem Metadata</param>
        /// <param name="proposedWatermark">The Proposed Watermark</param>
        /// <param name="procedureStepState">The Procedure Step State</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task representing the method status.</returns>
        Task UpdateWorkitemProcedureStepStateAsync(WorkitemMetadataStoreEntry workitemMetadata, long proposedWatermark, string procedureStepState, CancellationToken cancellationToken = default);

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
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Returns the Workitem attributes that are indexed in a store.</returns>
        Task<WorkitemMetadataStoreEntry> GetWorkitemMetadataAsync(int partitionKey, string workitemUid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets current and next workitem watermark
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="workitemUid">Workitem instance UID</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Returns the Current and Next Workitem Watermark.</returns>
        Task<(long CurrentWatermark, long NextWatermark)?> GetCurrentAndNextWorkitemWatermarkAsync(int partitionKey, string workitemUid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously queries workitem
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="query">Query expression that matches the filters</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that gets workitem that matches the query filters</returns>
        Task<WorkitemQueryResult> QueryAsync(int partitionKey, BaseQueryExpression query, CancellationToken cancellationToken = default);
    }
}
