// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// Represents an Reindexer which reindexes DICOM instance.
    /// </summary>
    public interface IInstanceReindexer
    {
        /// <summary>
        /// Asynchronously reindexes the DICOM instance with the <paramref name="versionedInstanceId"/>
        /// for the set of <paramref name="entries"/>.
        /// </summary>
        /// <param name="entries">Extended query tag store entries.</param>
        /// <param name="versionedInstanceId">The versioned instance id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The value of the
        /// <see cref="Task{TResult}.Result"/> indicates whether the reindexing was successful.
        /// </returns>
        Task<bool> ReindexInstanceAsync(IReadOnlyCollection<ExtendedQueryTagStoreEntry> entries, VersionedInstanceIdentifier versionedInstanceId, CancellationToken cancellationToken = default);
    }
}
