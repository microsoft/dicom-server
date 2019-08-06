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
    public interface IDicomMetadataStore
    {
        Task AddStudySeriesDicomMetadataAsync(IEnumerable<DicomDataset> instances, CancellationToken cancellationToken = default);

        Task<DicomDataset> GetStudyDicomMetadataWithAllOptionalAsync(string studyInstanceUID, CancellationToken cancellationToken = default);

        Task<DicomDataset> GetStudyDicomMetadataAsync(string studyInstanceUID, HashSet<DicomAttributeId> optionalAttributes = null, CancellationToken cancellationToken = default);

        Task<DicomDataset> GetSeriesDicomMetadataWithAllOptionalAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default);

        Task<DicomDataset> GetSeriesDicomMetadataAsync(
            string studyInstanceUID, string seriesInstanceUID, HashSet<DicomAttributeId> optionalAttributes = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<DicomInstance>> GetInstancesInStudyAsync(string studyInstanceUID, CancellationToken cancellationToken = default);

        Task<IEnumerable<DicomInstance>> GetInstancesInSeriesAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default);

        Task<IEnumerable<DicomInstance>> DeleteStudyAsync(string studyInstanceUID, CancellationToken cancellationToken = default);

        Task<IEnumerable<DicomInstance>> DeleteSeriesAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default);

        Task DeleteInstanceAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken = default);
    }
}
