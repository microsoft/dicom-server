// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage
{
    public interface IDicomMetadataStore
    {
        Task AddStudySeriesDicomMetadataAsync(DicomDataset instance, CancellationToken cancellationToken = default);

        Task<IEnumerable<DicomInstance>> GetInstancesInStudyAsync(string studyInstanceUID, CancellationToken cancellationToken = default);

        Task<IEnumerable<DicomInstance>> GetInstancesInSeriesAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default);

        Task<IEnumerable<DicomInstance>> DeleteStudyAsync(string studyInstanceUID, CancellationToken cancellationToken = default);

        Task<IEnumerable<DicomInstance>> DeleteSeriesAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default);

        Task DeleteInstanceAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken = default);
    }
}
