// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to manage UPS-RS workitems.
    /// </summary>
    public interface IWorkitemStore
    {
        /// <summary>
        /// Asynchronously adds a workitem instance.
        /// </summary>
        /// <param name="identifier">The workitem instance identifier.</param>
        /// <param name="dataset">The dicom dataset</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous add operation.</returns>
        Task AddWorkitemAsync(WorkitemInstanceIdentifier identifier, DicomDataset dataset, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a workitem instance.
        /// </summary>
        /// <param name="workitemInstanceIdentifier">The workitem instance identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get operation.</returns>
        Task<DicomDataset> GetWorkitemAsync(
            WorkitemInstanceIdentifier workitemInstanceIdentifier,
            CancellationToken cancellationToken = default);
    }
}
