// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to orchestrate the storing of a UPS-RS workitem.
    /// </summary>
    public interface IWorkitemOrchestrator
    {
        /// <summary>
        /// Asynchronously orchestrate the storing of a UPS-RS workitem.
        /// </summary>
        /// <param name="dataset">The workitem dataset to store.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous orchestration of the storing operation.</returns>
        Task AddWorkitemAsync(DicomDataset dataset, CancellationToken cancellationToken);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="workitemInstanceUid"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteWorkitemAsync(string workitemInstanceUid, CancellationToken cancellationToken);
    }
}
