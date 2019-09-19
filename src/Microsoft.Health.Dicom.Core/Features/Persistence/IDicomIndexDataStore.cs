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
        Task IndexSeriesAsync(IReadOnlyCollection<DicomDataset> series, CancellationToken cancellationToken = default);

        Task<QueryResult<DicomStudy>> QueryStudiesAsync(
            int offset,
            int limit,
            string studyInstanceUID = null,
            IEnumerable<(DicomAttributeId Attribute, string Value)> query = null,
            CancellationToken cancellationToken = default);

        Task<QueryResult<DicomSeries>> QuerySeriesAsync(
            int offset,
            int limit,
            string studyInstanceUID = null,
            IEnumerable<(DicomAttributeId Attribute, string Value)> query = null,
            CancellationToken cancellationToken = default);

        Task<QueryResult<DicomInstance>> QueryInstancesAsync(
            int offset,
            int limit,
            string studyInstanceUID = null,
            IEnumerable<(DicomAttributeId Attribute, string Value)> query = null,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<DicomInstance>> DeleteStudyIndexAsync(string studyInstanceUID, CancellationToken cancellationToken = default);

        Task<IEnumerable<DicomInstance>> DeleteSeriesIndexAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default);

        Task DeleteInstanceIndexAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken = default);
    }
}
