// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public interface IDicomIndexDataStore
    {
        Task IndexInstanceAsync(DicomDataset dicomDataset, CancellationToken cancellationToken = default);

        Task<IEnumerable<DicomIdentity>> QueryInstancesAsync(
            int offset,
            int limit,
            string studyInstanceUID = null,
            IEnumerable<(DicomTag Attribute, string Value)> query = null,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<DicomIdentity>> GetInstancesInStudyAsync(string studyInstanceUID, CancellationToken cancellationToken = default);

        Task<IEnumerable<DicomIdentity>> GetInstancesInSeriesAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete the provided series index.
        /// </summary>
        /// <param name="studyInstanceUID">The study instance unique identifier.</param>
        /// <param name="seriesInstanceUID">The series instance unique identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of instances that were deleted from this series.</returns>
        Task<IEnumerable<DicomIdentity>> DeleteSeriesIndexAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default);

        Task DeleteInstanceIndexAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken = default);
    }
}
