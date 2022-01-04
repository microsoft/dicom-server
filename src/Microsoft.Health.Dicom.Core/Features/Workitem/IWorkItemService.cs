// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to process the list of <see cref="WorkitemEntry"/>.
    /// </summary>
    public interface IWorkitemService
    {
        /// <summary>
        /// Asynchronously processes the ...
        /// </summary>
        /// <remarks>
        /// If the <paramref name="workitemInstanceUid"/> is not specified, a new workitemInstanceUid is created.
        /// </remarks>
        /// <param name="instanceEntry">The <see cref="IDicomInstanceEntry"/> to process.</param>
        /// <param name="workitemInstanceUid">An optional value for the Work Item InstanceUID tag.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous process operation.</returns>
        Task<WorkitemStoreResponse> ProcessAsync(
            IDicomInstanceEntry instanceEntry,
            string workitemInstanceUid,
            CancellationToken cancellationToken);
    }
}
