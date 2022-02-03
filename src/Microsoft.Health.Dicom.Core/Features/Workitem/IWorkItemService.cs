// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to process the <see cref="DicomDataset"/>.
    /// </summary>
    public interface IWorkitemService
    {
        /// <summary>
        /// Asynchronously processes the workitem dataset
        /// </summary>
        /// <remarks>
        /// If the <paramref name="workitemInstanceUid"/> is not specified, a new workitemInstanceUid is created.
        /// </remarks>
        /// <param name="dataset">The <see cref="DicomDataset"/> to process.</param>
        /// <param name="workitemInstanceUid">An optional value for the Work Item InstanceUID tag.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous process operation.</returns>
        Task<AddWorkitemResponse> ProcessAddAsync(
            DicomDataset dataset,
            string workitemInstanceUid,
            CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously process the searching of a UPS-RS workitem
        /// </summary>
        /// <param name="parameters">Query parameters that contains filters</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous process operation.</returns>
        Task<QueryWorkitemResourceResponse> ProcessQueryAsync(BaseQueryParameters parameters, CancellationToken cancellationToken = default);
    }
}
