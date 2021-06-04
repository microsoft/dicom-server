// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public interface IInstanceStore
    {
        Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifiersInStudyAsync(
            string studyInstanceUid,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifiersInSeriesAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifierAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets idenfiers of instances within the given range of watermarks.
        /// </summary>
        /// <param name="startWatermark">The inclusive start watermark</param>
        /// <param name="endWatermark">The inclusive end watermark</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The instanceidentifiers</returns>
        Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifiersAsync(long startWatermark, long endWatermark, CancellationToken cancellationToken = default);

    }
}
