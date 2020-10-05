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
        HttpClient HttpClient { get; }

        Task<DicomWebResponse> DeleteAsync(Uri requestUri, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<IReadOnlyList<ChangeFeedEntry>>> GetChangeFeed(string queryString = "", CancellationToken cancellationToken = default);

        Task<DicomWebResponse<ChangeFeedEntry>> GetChangeFeedLatest(string queryString = "", CancellationToken cancellationToken = default);

        Task<DicomWebResponse<DicomDataset>> PostMultipartContentAsync(MultipartContent multiContent, string requestUri, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<DicomDataset>> PostSinglepartConentAsync(StreamContent streamContent, string requestUri, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<IEnumerable<DicomDataset>>> QueryAsync(string requestUri, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<string>> QueryWithBadRequest(string requestUri, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveFramesAsync(Uri requestUri, string mediaType = DicomWebConstants.ApplicationOctetStreamMediaType, string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<IReadOnlyList<DicomFile>>> RetrieveInstancesAsync(Uri requestUri, bool singleInstance = false, string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<IReadOnlyList<DicomDataset>>> RetrieveMetadataAsync(Uri requestUri, string ifNoneMatch = null, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<DicomDataset>> StoreAsync(IEnumerable<DicomFile> dicomFiles, string studyInstanceUid = null, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<DicomDataset>> StoreAsync(IEnumerable<Stream> streams, string studyInstanceUid = null, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<DicomDataset>> StoreAsync(DicomFile dicomFile, string studyInstanceUid = null, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<DicomDataset>> StoreAsync(Stream stream, string studyInstanceUid = null, CancellationToken cancellationToken = default);
    }
}
