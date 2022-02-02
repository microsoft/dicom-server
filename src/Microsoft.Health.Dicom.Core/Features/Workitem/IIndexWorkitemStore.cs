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
        /// Asynchronously creates a workitem instance.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="dataset">The DICOM dataset to index.</param>
        /// <param name="queryTags">Queryable workitem tags</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that gets the workitem key.</returns>
        Task<long> AddWorkitemAsync(int partitionKey, DicomDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deletes a workitem instance.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="workitemUid">Workitem instance UID</param>
        /// <param name="dataset">The DICOM dataset to index.</param>
        /// <param name="queryTags">Queryable workitem tags</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task representing the method status.</returns>
        Task<long> UpdateWorkitemAsync(int partitionKey, string workitemUid, DicomDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default);

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
        /// Asynchronously gets workitem detail
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="workitemUid">Workitem instance UID</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that gets workitem query tags.</returns>
        Task<WorkitemDetail> GetWorkitemDetailAsync(int partitionKey, string workitemUid, CancellationToken cancellationToken = default);
    }
}
