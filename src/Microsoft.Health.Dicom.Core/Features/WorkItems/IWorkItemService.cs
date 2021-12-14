// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Messages.WorkItemMessages;

namespace Microsoft.Health.Dicom.Core.Features.WorkItems
{
    public interface IWorkItemService
    {
        /// <summary>
        /// Asynchronously processes the ...
        /// </summary>
        /// <remarks>
        /// If the <paramref name="requiredWorkItemInstanceUid"/> is specified, every element in the
        /// </remarks>
        /// <param name="requiredWorkItemInstanceUid">An optional value for the Work Item InstanceUID tag.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous process operation.</returns>
        Task<WorkItemStoreResponse> ProcessAsync(
            string requiredWorkItemInstanceUid,
            CancellationToken cancellationToken);
    }
}
