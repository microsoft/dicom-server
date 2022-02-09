// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to orchestrate the adding of a UPS-RS workitem.
    /// </summary>
    public interface IWorkitemOrchestrator
    {
        /// <summary>
        /// Gets Workitem metadata from the store
        /// </summary>
        /// <param name="workitemInstanceUid">The workitem instance UID</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<WorkitemMetadataStoreEntry> GetWorkitemMetadataAsync(string workitemInstanceUid, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously orchestrate the adding of a UPS-RS workitem.
        /// </summary>
        /// <param name="dataset">The workitem dataset to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous orchestration of the adding operation.</returns>
        Task AddWorkitemAsync(DicomDataset dataset, CancellationToken cancellationToken);


        /// <summary>
        /// Asynchronously orchestrate the canceling of a UPS-RS workitem.
        /// </summary>
        /// <param name="dataset">The workitem dataset to add.</param>
        /// <param name="workitemMetadata">The workitem metadata</param>
        /// <param name="targetProcedureStepState">The target procedure step state</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task CancelWorkitemAsync(DicomDataset dataset, WorkitemMetadataStoreEntry workitemMetadata, ProcedureStepState targetProcedureStepState, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously orchestrate the searching of a UPS-RS workitem
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<QueryWorkitemResourceResponse> QueryAsync(BaseQueryParameters parameters, CancellationToken cancellationToken = default);
    }
}
