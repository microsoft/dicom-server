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
    public class DicomWebClient : IDicomWebClient
    {
        private const string TransferSyntaxHeaderName = "transfer-syntax";

        private const string BaseExtendedQueryTagUri = "/tags";

        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public DicomWebClient(HttpClient httpClient)
        {
            EnsureArg.IsNotNull(httpClient, nameof(httpClient));

            HttpClient = httpClient;

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

        public async Task<DicomWebResponse<DicomFile>> RetrieveInstanceAsync(
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

        public async Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveInstancesAsync(
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

        public async Task<DicomWebAsyncEnumerableResponse<Stream>> RetrieveFramesAsync(
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

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveMetadataAsync(
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

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
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

        public async Task<DicomWebResponse> DeleteAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebResponse(response);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryAsync(string requestUri, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri(requestUri, UriKind.Relative));

            request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebAsyncEnumerableResponse<DicomDataset>(
                response,
                DeserializeAsAsyncEnumerable<DicomDataset>(response.Content));
        }

        public async Task<DicomWebAsyncEnumerableResponse<ChangeFeedEntry>> GetChangeFeed(string queryString, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri($"/changefeed{queryString}", UriKind.Relative));

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
                new Uri($"/changefeed/latest{queryString}", UriKind.Relative));

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

        public async Task<DicomWebResponse> AddExtendedQueryTagAsync(IEnumerable<ExtendedQueryTagEntry> tagEntries, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(tagEntries, nameof(tagEntries));
            using var request = new HttpRequestMessage(HttpMethod.Post, BaseExtendedQueryTagUri);
            {
                string jsonString = JsonConvert.SerializeObject(tagEntries);
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

            using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri($"{BaseExtendedQueryTagUri}/{tagPath}", UriKind.Relative));

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebResponse(response);
        }

        public async Task<DicomWebResponse<IEnumerable<ExtendedQueryTagEntry>>> GetExtendedQueryTagsAsync(CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(BaseExtendedQueryTagUri, UriKind.Relative));
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
            return new DicomWebResponse<IEnumerable<ExtendedQueryTagEntry>>(
                 response,
                 async content =>
                 {
                     string contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                     return JsonConvert.DeserializeObject<IEnumerable<ExtendedQueryTagEntry>>(contentText, _jsonSerializerSettings);
                 });
        }

        public async Task<DicomWebResponse<ExtendedQueryTagEntry>> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{BaseExtendedQueryTagUri}/{tagPath}", UriKind.Relative));
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
            return new DicomWebResponse<ExtendedQueryTagEntry>(
                 response,
                 async content =>
                 {
                     string contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                     return JsonConvert.DeserializeObject<ExtendedQueryTagEntry>(contentText, _jsonSerializerSettings);
                 });
        }

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

        private static string CreateAcceptHeader(MediaTypeWithQualityHeaderValue mediaTypeHeader, string dicomTransferSyntax)
        {
            string transferSyntaxHeader = dicomTransferSyntax == null ? string.Empty : $";{TransferSyntaxHeaderName}=\"{dicomTransferSyntax}\"";

            return $"{mediaTypeHeader}{transferSyntaxHeader}";
        }

        private static MediaTypeWithQualityHeaderValue CreateMultipartMediaTypeHeader(string contentType)
        {
            var multipartHeader = new MediaTypeWithQualityHeaderValue(DicomWebConstants.MultipartRelatedMediaType);
            var contentHeader = new NameValueHeaderValue("type", "\"" + contentType + "\"");

            multipartHeader.Parameters.Add(contentHeader);
            return multipartHeader;
        }

        private static Uri GenerateStoreRequestUri(string studyInstanceUid)
            => new Uri(string.Format(DicomWebConstants.BaseStudyUriFormat, studyInstanceUid), UriKind.Relative);

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
    }
}
