// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Client
{
    public partial interface IDicomWebClient
    {
        Task<DicomWebResponse> DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string partitionName = default, CancellationToken cancellationToken = default);
        Task<DicomWebResponse> DeleteSeriesAsync(string studyInstanceUid, string seriesInstanceUid, string partitionName = default, CancellationToken cancellationToken = default);
        Task<DicomWebResponse> DeleteStudyAsync(string studyInstanceUid, string partitionName = default, CancellationToken cancellationToken = default);
    }
}
