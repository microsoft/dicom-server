// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
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
        /// If the <paramref name="requiredWorkitemInstanceUid"/> is specified, every element in the
        /// </remarks>
        /// <param name="instanceEntries">The list of <see cref="IDicomInstanceEntry"/> to process.</param>
        /// <param name="requiredWorkitemInstanceUid">An optional value for the Work Item InstanceUID tag.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous process operation.</returns>
        Task<WorkitemStoreResponse> ProcessAsync(
            IReadOnlyList<IDicomInstanceEntry> instanceEntries,
            string requiredWorkitemInstanceUid,
            CancellationToken cancellationToken);
    }
}
