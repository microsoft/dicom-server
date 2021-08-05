// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Dicom.Serialization;
using EnsureThat;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using MediaTypeHeaderValue = Microsoft.Net.Http.Headers.MediaTypeHeaderValue;
using NameValueHeaderValue = System.Net.Http.Headers.NameValueHeaderValue;

namespace Microsoft.Health.Dicom.Client
{
    public partial class DicomWebClient : IDicomWebClient
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly string _apiVersion;

        /// <summary>
        /// New instance of DicomWebClient to talk to the server
        /// </summary>
        /// <param name="httpClient">HttpClient</param>
        /// <param name="apiVersion">Pin the DicomWebClient to a specific server API version.</param>
        public DicomWebClient(HttpClient httpClient, string apiVersion = DicomApiVersions.V1Prerelease)
        {
            EnsureArg.IsNotNull(httpClient, nameof(httpClient));

            HttpClient = httpClient;
            _apiVersion = apiVersion;
            _jsonSerializerSettings = new JsonSerializerSettings();
            _jsonSerializerSettings.Converters.Add(new JsonDicomConverter(writeTagsAsKeywords: true, autoValidate: false));

            GetMemoryStream = () => new MemoryStream();
        }

        public HttpClient HttpClient { get; }

        /// <summary>
        /// Func used to obtain a <see cref="MemoryStream" />. The default value returns a new memory stream.
        /// </summary>
        /// <remarks>
        /// This can be used in conjunction with a memory stream pool.
        /// </remarks>
        public Func<MemoryStream> GetMemoryStream { get; set; }

        #region WADO-RS
        public async Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveStudyAsync(
            string studyInstanceUid,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            return await RetrieveInstancesAsync(
                new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseStudyUriFormat, studyInstanceUid), UriKind.Relative),
                dicomTransferSyntax,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveStudyMetadataAsync(
            string studyInstanceUid,
            string ifNoneMatch = null,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            return await RetrieveMetadataAsync(
                new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseRetrieveStudyMetadataUriFormat, studyInstanceUid), UriKind.Relative),
                ifNoneMatch,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveSeriesAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

            return await RetrieveInstancesAsync(
                new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseSeriesUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative),
                dicomTransferSyntax,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveSeriesMetadataAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string ifNoneMatch = null,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

            return await RetrieveMetadataAsync(
                new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseRetrieveSeriesMetadataUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative),
                ifNoneMatch,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebResponse<DicomFile>> RetrieveInstanceAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));

            return await RetrieveInstanceAsync(
                new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseInstanceUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative),
                dicomTransferSyntax,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveInstanceMetadataAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string ifNoneMatch = null,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));

            return await RetrieveMetadataAsync(
                new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseRetrieveInstanceMetadataUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative),
                ifNoneMatch,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<Stream>> RetrieveFramesAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            int[] frames = null,
            string mediaType = DicomWebConstants.ApplicationOctetStreamMediaType,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));
            var requestUri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseRetrieveFramesUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid, string.Join("%2C", frames)), UriKind.Relative);

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
        #endregion

        #region STOW-RS
        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            IEnumerable<DicomFile> dicomFiles,
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomFiles, nameof(dicomFiles));

            var postContent = new List<Stream>();

            try
            {
                foreach (DicomFile dicomFile in dicomFiles)
                {
                    MemoryStream stream = GetMemoryStream();
                    await dicomFile.SaveAsync(stream).ConfigureAwait(false);
                    stream.Seek(0, SeekOrigin.Begin);
                    postContent.Add(stream);
                }

                using MultipartContent content = ConvertStreamsToMultipartContent(postContent);
                return await StoreAsync(
                    GenerateStoreRequestUri(studyInstanceUid),
                    content,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                foreach (Stream stream in postContent)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            IEnumerable<Stream> streams,
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(streams, nameof(streams));

            return await StoreAsync(
                GenerateStoreRequestUri(studyInstanceUid),
                ConvertStreamsToMultipartContent(streams),
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            Stream stream,
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(stream, nameof(stream));

            stream.Seek(0, SeekOrigin.Begin);

            return await StoreAsync(
                GenerateStoreRequestUri(studyInstanceUid),
                ConvertStreamToStreamContent(stream),
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            DicomFile dicomFile,
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));

            await using MemoryStream stream = GetMemoryStream();
            await dicomFile.SaveAsync(stream).ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);

            using StreamContent content = ConvertStreamToStreamContent(stream);
            return await StoreAsync(
                GenerateStoreRequestUri(studyInstanceUid),
                content,
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            HttpContent content,
            CancellationToken cancellationToken)
        {
            return await StoreAsync(
                new Uri("/" + _apiVersion + DicomWebConstants.StudiesUriString, UriKind.Relative),
                content,
                cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<DicomWebResponse<DicomDataset>> StoreAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));
            EnsureArg.IsNotNull(content, nameof(content));

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

            request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);

            request.Content = content;

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(
                response,
                additionalFailureInspector: (statusCode, responseHeaders, contentHeaders, responseBody) =>
                {
                    // If store fails, we will get Conflict status code but the body will be a DicomDataset,
                    // so we need to handle this case specially.
                    if (statusCode == HttpStatusCode.Conflict)
                    {
                        throw new DicomWebException(
                            statusCode,
                            responseHeaders,
                            contentHeaders,
                            JsonConvert.DeserializeObject<DicomDataset>(responseBody, _jsonSerializerSettings));
                    }

                    return false;
                })
                .ConfigureAwait(false);

            return new DicomWebResponse<DicomDataset>(
                response,
                async content =>
                {
                    string contentText = await content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<DicomDataset>(contentText, _jsonSerializerSettings);
                });
        }
        #endregion

        #region Delete
        public async Task<DicomWebResponse> DeleteStudyAsync(
            string studyInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            var requestUri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseStudyUriFormat, studyInstanceUid), UriKind.Relative);

            return await DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebResponse> DeleteSeriesAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

            var requestUri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseSeriesUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative);

            return await DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebResponse> DeleteInstanceAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));

            var requestUri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseInstanceUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative);

            return await DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        }

        private async Task<DicomWebResponse> DeleteAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebResponse(response);
        }
        #endregion

        #region QIDO-RS
        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyAsync(
            string queryString,
            CancellationToken cancellationToken)
        {
            var uri = new Uri("/" + _apiVersion + DicomWebConstants.StudiesUriString + GetQueryParamUriString(queryString), UriKind.Relative);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudySeriesAsync(
            string studyInstanceUid,
            string queryString,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            var uri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.QueryStudySeriesUriFormat, studyInstanceUid) + GetQueryParamUriString(queryString), UriKind.Relative);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyInstanceAsync(
            string studyInstanceUid,
            string queryString,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            var uri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.QueryStudyInstanceUriFormat, studyInstanceUid) + GetQueryParamUriString(queryString), UriKind.Relative);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudySeriesInstanceAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string queryString,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

            var uri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.QueryStudySeriesInstancesUriFormat, studyInstanceUid, seriesInstanceUid) + GetQueryParamUriString(queryString), UriKind.Relative);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesAsync(
            string queryString,
            CancellationToken cancellationToken)
        {
            var uri = new Uri("/" + _apiVersion + DicomWebConstants.SeriesUriString + GetQueryParamUriString(queryString), UriKind.Relative);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesInstanceAsync(
            string seriesInstanceUid,
            string queryString,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

            var uri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.QuerySeriesInstancUriFormat, seriesInstanceUid) + GetQueryParamUriString(queryString), UriKind.Relative);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryInstancesAsync(
            string queryString,
            CancellationToken cancellationToken)
        {
            var uri = new Uri("/" + _apiVersion + DicomWebConstants.InstancesUriString + GetQueryParamUriString(queryString), UriKind.Relative);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        private async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryAsync(
            Uri requestUri,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebAsyncEnumerableResponse<DicomDataset>(
                response,
                DeserializeAsAsyncEnumerable<DicomDataset>(response.Content));
        }
        #endregion

        #region ChangeFeed
        public async Task<DicomWebAsyncEnumerableResponse<ChangeFeedEntry>> GetChangeFeed(string queryString, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri($"/{_apiVersion}/changefeed{queryString}", UriKind.Relative));

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebAsyncEnumerableResponse<ChangeFeedEntry>(
                response,
                DeserializeAsAsyncEnumerable<ChangeFeedEntry>(response.Content));
        }

        public async Task<DicomWebResponse<ChangeFeedEntry>> GetChangeFeedLatest(string queryString, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri($"/{_apiVersion}/changefeed/latest{queryString}", UriKind.Relative));

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebResponse<ChangeFeedEntry>(
                response,
                async content =>
                {
                    string contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<ChangeFeedEntry>(contentText, _jsonSerializerSettings);
                });
        }
        #endregion

        #region ExtendedQueryTag
        public async Task<DicomWebResponse> AddExtendedQueryTagAsync(IEnumerable<AddExtendedQueryTagEntry> tagEntries, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(tagEntries, nameof(tagEntries));
            string jsonString = JsonConvert.SerializeObject(tagEntries);
            var uri = new Uri($"/{_apiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}", UriKind.Relative);
            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            {
                request.Content = new StringContent(jsonString);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
            }

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
            return new DicomWebResponse(response);
        }

        public async Task<DicomWebResponse> DeleteExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(tagPath, nameof(tagPath));

            var uri = new Uri($"/{_apiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}/{tagPath}", UriKind.Relative);
            using var request = new HttpRequestMessage(HttpMethod.Delete, uri);

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebResponse(response);
        }

        public async Task<DicomWebResponse<IEnumerable<GetExtendedQueryTagEntry>>> GetExtendedQueryTagsAsync(CancellationToken cancellationToken)
        {
            var uri = new Uri($"/{_apiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}", UriKind.Relative);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
            return new DicomWebResponse<IEnumerable<GetExtendedQueryTagEntry>>(
                 response,
                 async content =>
                 {
                     string contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                     return JsonConvert.DeserializeObject<IEnumerable<GetExtendedQueryTagEntry>>(contentText, _jsonSerializerSettings);
                 });
        }

        public async Task<DicomWebResponse<GetExtendedQueryTagEntry>> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            var uri = new Uri($"/{_apiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}/{tagPath}", UriKind.Relative);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
            return new DicomWebResponse<GetExtendedQueryTagEntry>(
                 response,
                 async content =>
                 {
                     string contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                     return JsonConvert.DeserializeObject<GetExtendedQueryTagEntry>(contentText, _jsonSerializerSettings);
                 });
        }
        #endregion

        #region Helpers
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Callers will dispose of the StreamContent")]
        private static MultipartContent ConvertStreamsToMultipartContent(IEnumerable<Stream> streams)
        {
            var multiContent = new MultipartContent("related");

            multiContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

            foreach (Stream stream in streams)
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;
                multiContent.Add(streamContent);
            }

            return multiContent;
        }

        private static StreamContent ConvertStreamToStreamContent(Stream stream)
        {
            var streamContent = new StreamContent(stream);

            streamContent.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;

            return streamContent;
        }

        private static string GetQueryParamUriString(string queryString)
        {
            return string.IsNullOrWhiteSpace(queryString) == true ? string.Empty : "?" + queryString;
        }

        private static string CreateAcceptHeader(MediaTypeWithQualityHeaderValue mediaTypeHeader, string dicomTransferSyntax)
        {
            string transferSyntaxHeader = dicomTransferSyntax == null ? string.Empty : $";{DicomWebConstants.TransferSyntaxHeaderName}=\"{dicomTransferSyntax}\"";

            return $"{mediaTypeHeader}{transferSyntaxHeader}";
        }

        private static MediaTypeWithQualityHeaderValue CreateMultipartMediaTypeHeader(string contentType)
        {
            var multipartHeader = new MediaTypeWithQualityHeaderValue(DicomWebConstants.MultipartRelatedMediaType);
            var contentHeader = new NameValueHeaderValue("type", "\"" + contentType + "\"");

            multipartHeader.Parameters.Add(contentHeader);
            return multipartHeader;
        }

        private Uri GenerateStoreRequestUri(string studyInstanceUid)
            => new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseStudyUriFormat, studyInstanceUid), UriKind.Relative);

        private async IAsyncEnumerable<Stream> ReadMultipartResponseAsStreamsAsync(HttpContent httpContent, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(httpContent, nameof(httpContent));

            await using Stream stream = await httpContent.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            MultipartSection part;

            var media = MediaTypeHeaderValue.Parse(httpContent.Headers.ContentType.ToString());
            var multipartReader = new MultipartReader(HeaderUtilities.RemoveQuotes(media.Boundary).Value, stream, 100);

            while ((part = await multipartReader.ReadNextSectionAsync(cancellationToken).ConfigureAwait(false)) != null)
            {
                MemoryStream memoryStream = GetMemoryStream();
                await part.Body.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                memoryStream.Seek(0, SeekOrigin.Begin);

                yield return memoryStream;
            }
        }

        private static async Task EnsureSuccessStatusCodeAsync(
            HttpResponseMessage response,
            Func<HttpStatusCode, HttpResponseHeaders, HttpContentHeaders, string, bool> additionalFailureInspector = null)
        {
            if (!response.IsSuccessStatusCode)
            {
                HttpStatusCode statusCode = response.StatusCode;
                HttpResponseHeaders responseHeaders = response.Headers;
                HttpContentHeaders contentHeaders = response.Content?.Headers;
                string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                Exception exception = null;

                try
                {
                    bool handled = additionalFailureInspector?.Invoke(statusCode, responseHeaders, contentHeaders, responseBody) ?? false;

                    if (!handled)
                    {
                        throw new DicomWebException(statusCode, responseHeaders, contentHeaders, responseBody);
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    // If we are throwing exception, then we can close the response because we have already read the body.
                    if (exception != null)
                    {
                        response.Dispose();
                    }
                }

                if (exception != null)
                {
                    throw exception;
                }
            }
        }

        private async IAsyncEnumerable<DicomFile> ReadMultipartResponseAsDicomFileAsync(HttpContent content, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (Stream stream in ReadMultipartResponseAsStreamsAsync(content, cancellationToken).ConfigureAwait(false))
            {
                yield return await DicomFile.OpenAsync(stream).ConfigureAwait(false);
            }
        }

        private async IAsyncEnumerable<T> DeserializeAsAsyncEnumerable<T>(HttpContent content)
        {
            string contentText = await content.ReadAsStringAsync().ConfigureAwait(false);

            if (string.IsNullOrEmpty(contentText))
            {
                yield break;
            }

            foreach (T item in JsonConvert.DeserializeObject<IReadOnlyList<T>>(contentText, _jsonSerializerSettings))
            {
                yield return item;
            }
        }
        #endregion
    }
}
