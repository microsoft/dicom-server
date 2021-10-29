// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Client
{
    public partial interface IDicomWebClient
    {
        Task<DicomWebAsyncEnumerableResponse<Stream>> RetrieveFramesAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int[] frames = default, string mediaType = DicomWebConstants.ApplicationOctetStreamMediaType, string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax, string partitionName = default, CancellationToken cancellationToken = default);
        Task<DicomWebResponse<DicomFile>> RetrieveInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax, string partitionName = default, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveInstanceMetadataAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string ifNoneMatch = default, string partitionName = default, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveSeriesAsync(string studyInstanceUid, string seriesInstanceUid, string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax, string partitionName = default, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveSeriesMetadataAsync(string studyInstanceUid, string seriesInstanceUid, string ifNoneMatch = default, string partitionName = default, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveStudyAsync(string studyInstanceUid, string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax, string partitionName = default, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveStudyMetadataAsync(string studyInstanceUid, string ifNoneMatch = default, string partitionName = default, CancellationToken cancellationToken = default);
    }
}
