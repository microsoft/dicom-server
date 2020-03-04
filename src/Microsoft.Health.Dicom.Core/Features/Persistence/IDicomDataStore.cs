// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Persistence.Store;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public interface IDicomDataStore
    {
        StoreTransaction BeginStoreTransaction();

        Task<Stream> GetDicomDataStreamAsync(DicomInstance dicomInstance, CancellationToken cancellationToken = default);

        Task DeleteStudyAsync(string studyInstanceUID, CancellationToken cancellationToken = default);

        Task DeleteSeriesAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default);

        Task DeleteInstanceAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken = default);

        Task DeleteInstanceMetadataAndBlobsAsync(IEnumerable<DicomInstance> instances, CancellationToken cancellationToken = default);
    }
}
