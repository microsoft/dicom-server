// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Client
{
    public partial class DicomWebClient : IDicomWebClient
    {
        public async Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveStudyAsync(
            string studyInstanceUid,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            string partitionName = default,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            return await RetrieveInstancesAsync(
                GenerateRequestUri(string.Format(DicomWebConstants.BaseStudyUriFormat, studyInstanceUid), partitionName),
                dicomTransferSyntax,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveStudyMetadataAsync(
            string studyInstanceUid,
            string ifNoneMatch = default,
            string partitionName = default,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            return await RetrieveMetadataAsync(
                GenerateRequestUri(string.Format(DicomWebConstants.BaseRetrieveStudyMetadataUriFormat, studyInstanceUid), partitionName),
                ifNoneMatch,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveSeriesAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            string partitionName = default,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

            return await RetrieveInstancesAsync(
                GenerateRequestUri(string.Format(DicomWebConstants.BaseSeriesUriFormat, studyInstanceUid, seriesInstanceUid), partitionName),
                dicomTransferSyntax,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveSeriesMetadataAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string ifNoneMatch = default,
            string partitionName = default,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

            return await RetrieveMetadataAsync(
                GenerateRequestUri(string.Format(DicomWebConstants.BaseRetrieveSeriesMetadataUriFormat, studyInstanceUid, seriesInstanceUid), partitionName),
                ifNoneMatch,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebResponse<DicomFile>> RetrieveInstanceAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            string partitionName = default,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));

            return await RetrieveInstanceAsync(
                GenerateRequestUri(string.Format(DicomWebConstants.BaseInstanceUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), partitionName),
                dicomTransferSyntax,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveInstanceMetadataAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string ifNoneMatch = default,
            string partitionName = default,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));

            return await RetrieveMetadataAsync(
                GenerateRequestUri(string.Format(DicomWebConstants.BaseRetrieveInstanceMetadataUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), partitionName),
                ifNoneMatch,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<Stream>> RetrieveFramesAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            int[] frames = default,
            string mediaType = DicomWebConstants.ApplicationOctetStreamMediaType,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            string partitionName = default,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));
            var requestUri = GenerateRequestUri(
                string.Format(
                    DicomWebConstants.BaseRetrieveFramesUriFormat,
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid,
                    string.Join("%2C", frames)),
                partitionName);
            return await RetrieveFramesAsync(requestUri, mediaType, dicomTransferSyntax, cancellationToken).ConfigureAwait(false);
        }

        private async Task<DicomWebResponse<DicomFile>> RetrieveInstanceAsync(
            Uri requestUri,
            string dicomTransferSyntax,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            request.Headers.TryAddWithoutValidation(
                "Accept",
                CreateAcceptHeader(DicomWebConstants.MediaTypeApplicationDicom, dicomTransferSyntax));

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebResponse<DicomFile>(
                response,
                async content =>
                {
                    MemoryStream memoryStream = GetMemoryStream();
                    await content.CopyToAsync(memoryStream).ConfigureAwait(false);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    return await DicomFile.OpenAsync(memoryStream).ConfigureAwait(false);
                });
        }

        private async Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveInstancesAsync(
            Uri requestUri,
            string dicomTransferSyntax,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            request.Headers.TryAddWithoutValidation(
                "Accept",
                CreateAcceptHeader(CreateMultipartMediaTypeHeader(DicomWebConstants.ApplicationDicomMediaType), dicomTransferSyntax));

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebAsyncEnumerableResponse<DicomFile>(
                response,
                ReadMultipartResponseAsDicomFileAsync(response.Content, cancellationToken));
        }

        private async Task<DicomWebAsyncEnumerableResponse<Stream>> RetrieveFramesAsync(
            Uri requestUri,
            string mediaType,
            string dicomTransferSyntax,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));
            EnsureArg.IsNotNullOrWhiteSpace(mediaType, nameof(mediaType));

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            request.Headers.TryAddWithoutValidation(
                "Accept",
                CreateAcceptHeader(CreateMultipartMediaTypeHeader(mediaType), dicomTransferSyntax));

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebAsyncEnumerableResponse<Stream>(
                response,
                ReadMultipartResponseAsStreamsAsync(response.Content, cancellationToken));
        }

        private async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveMetadataAsync(
            Uri requestUri,
            string ifNoneMatch,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);

            if (!string.IsNullOrEmpty(ifNoneMatch))
            {
                request.Headers.TryAddWithoutValidation(HeaderNames.IfNoneMatch, ifNoneMatch);
            }

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(
                response,
                additionalFailureInspector: (statusCode, responseHeaders, contentHeaders, responseBody) =>
                {
                    // If the content has not changed, the status returned will be NotModified and so we need to treat it specially.
                    return statusCode == HttpStatusCode.NotModified;
                })
                .ConfigureAwait(false);

            return new DicomWebAsyncEnumerableResponse<DicomDataset>(
                response,
                DeserializeAsAsyncEnumerable<DicomDataset>(response.Content));
        }
    }
}
