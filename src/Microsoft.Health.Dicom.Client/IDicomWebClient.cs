// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Client
{
    public interface IDicomWebClient
    {
        Func<MemoryStream> GetMemoryStream { get; set; }
        HttpClient HttpClient { get; }

        Task<DicomWebResponse> AddExtendedQueryTagAsync(IEnumerable<AddExtendedQueryTagEntry> tagEntries, CancellationToken cancellationToken = default);
        Task<DicomWebResponse> DeleteExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken = default);
        Task<DicomWebResponse> DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default);
        Task<DicomWebResponse> DeleteSeriesAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken = default);
        Task<DicomWebResponse> DeleteStudyAsync(string studyInstanceUid, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<ChangeFeedEntry>> GetChangeFeed(string queryString = "", CancellationToken cancellationToken = default);
        Task<DicomWebResponse<ChangeFeedEntry>> GetChangeFeedLatest(string queryString = "", CancellationToken cancellationToken = default);
        Task<DicomWebResponse<GetExtendedQueryTagEntry>> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken = default);
        Task<DicomWebResponse<IEnumerable<GetExtendedQueryTagEntry>>> GetExtendedQueryTagsAsync(CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryInstancesAsync(string queryString, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesAsync(string queryString, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesInstanceAsync(string seriesInstanceUid, string queryString, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyAsync(string queryString, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyInstanceAsync(string studyInstanceUid, string queryString, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudySeriesAsync(string studyInstanceUid, string queryString, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudySeriesInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string queryString, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<Stream>> RetrieveFramesAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int[] frames = null, string mediaType = DicomWebConstants.ApplicationOctetStreamMediaType, string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax, CancellationToken cancellationToken = default);
        Task<DicomWebResponse<DicomFile>> RetrieveInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveInstanceMetadataAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string ifNoneMatch = null, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveSeriesAsync(string studyInstanceUid, string seriesInstanceUid, string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveSeriesMetadataAsync(string studyInstanceUid, string seriesInstanceUid, string ifNoneMatch = null, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveStudyAsync(string studyInstanceUid, string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveStudyMetadataAsync(string studyInstanceUid, string ifNoneMatch = null, CancellationToken cancellationToken = default);
        Task<DicomWebResponse<DicomDataset>> StoreAsync(DicomFile dicomFile, string studyInstanceUid = null, CancellationToken cancellationToken = default);
        Task<DicomWebResponse<DicomDataset>> StoreAsync(HttpContent content, CancellationToken cancellationToken = default);
        Task<DicomWebResponse<DicomDataset>> StoreAsync(IEnumerable<DicomFile> dicomFiles, string studyInstanceUid = null, CancellationToken cancellationToken = default);
        Task<DicomWebResponse<DicomDataset>> StoreAsync(IEnumerable<Stream> streams, string studyInstanceUid = null, CancellationToken cancellationToken = default);
        Task<DicomWebResponse<DicomDataset>> StoreAsync(Stream stream, string studyInstanceUid = null, CancellationToken cancellationToken = default);
    }
}
