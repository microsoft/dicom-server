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

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// Provides functionality to orchestrate the adding of a UPS-RS workitem.
/// </summary>
public interface IWorkitemOrchestrator
{
    /// <summary>
    /// Gets Workitem metadata from the store
    /// </summary>
    /// <param name="workitemUid">The workitem instance UID</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<WorkitemMetadataStoreEntry> GetWorkitemMetadataAsync(
        string workitemUid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously orchestrate the adding of a UPS-RS workitem.
    /// </summary>
    /// <param name="dataset">The workitem dataset to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous orchestration of the adding operation.</returns>
    Task AddWorkitemAsync(DicomDataset dataset, CancellationToken cancellationToken);


    /// <summary>
    /// Asynchronously orchestrate updating the state of a UPS-RS workitem.
    /// </summary>
    /// <param name="dataset">The workitem dataset with the cancel request.</param>
    /// <param name="workitemMetadata">The workitem metadata</param>
    /// <param name="targetProcedureStepState">The target procedure step state</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task UpdateWorkitemStateAsync(DicomDataset dataset, WorkitemMetadataStoreEntry workitemMetadata, ProcedureStepState targetProcedureStepState, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously orchestrate the searching of a UPS-RS workitem
    /// </summary>
    /// <param name="parameters">The query parameters</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns></returns>
    Task<QueryWorkitemResourceResponse> QueryAsync(BaseQueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously orchestrate the retrieval of a UPS-RS workitem
    /// </summary>
    /// <param name="workitemInstanceIdentifier">The workitem instance identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task that represents the asynchronous orchestration of the retrieving a workitem DICOM dataset.</returns>
    Task<DicomDataset> RetrieveWorkitemAsync(WorkitemInstanceIdentifier workitemInstanceIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets DicomDataset Blob from the Store for the given Workitem Instance identifier
    /// </summary>
    /// <param name="identifier">The workitem instance identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task that retrieves a workitem DICOM dataset from the Blob storage.</returns>
    Task<DicomDataset> GetWorkitemBlobAsync(WorkitemInstanceIdentifier identifier, CancellationToken cancellationToken = default);
}
