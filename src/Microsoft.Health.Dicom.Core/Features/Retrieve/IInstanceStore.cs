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

        Task<string> GetETagForStudyAsync(
            string studyInstanceUid,
            CancellationToken cancellationToken);

        Task<string> GetETagForSeriesAsync(
            string seriesInstanceUid,
            CancellationToken cancellationToken);

        Task<string> GetETagForInstanceAsync(
            string sopInstanceUid,
            CancellationToken cancellationToken);
    }
}
