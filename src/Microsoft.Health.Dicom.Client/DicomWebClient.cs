// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        public static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationDicom = new MediaTypeWithQualityHeaderValue(ApplicationDicomContentType);
        public static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationOctetStream = new MediaTypeWithQualityHeaderValue(ApplicationOctetStreamContentType);
        public static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationDicomJson = new MediaTypeWithQualityHeaderValue(ApplicationDicomJsonContentType);

        private const string ApplicationDicomContentType = "application/dicom";
        private const string ApplicationDicomJsonContentType = "application/dicom+json";
        private const string ApplicationOctetStreamContentType = "application/octet-stream";
        private const string MultipartRelatedContentType = "multipart/related";
        private const string TransferSyntaxHeaderName = "transfer-syntax";
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        static DicomWebClient()
        {
#pragma warning disable CS0618 // Type or member is obsolete

            // Disable global DicomValidation to solve bug https://microsofthealth.visualstudio.com/Health/_workitems/edit/75104 until fodicom issue https://github.com/fo-dicom/fo-dicom/issues/974 is solved
            DicomValidation.AutoValidation = false;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public DicomWebClient(HttpClient httpClient)
        {
            HttpClient = httpClient;
            _jsonSerializerSettings = new JsonSerializerSettings();
            _jsonSerializerSettings.Converters.Add(new JsonDicomConverter(writeTagsAsKeywords: true));
            GetMemoryStream = () => new MemoryStream();
        }

        public HttpClient HttpClient { get; }

        /// <summary>
        /// Func used to obtain a <see cref="MemoryStream" />. The default value returns a new memory stream.
        /// This can be used in conjunction with a <see cref="RecyclableMemoryStreamManager"/> to provide a way to obtain a memory stream from the pool.
        /// </summary>
        public Func<MemoryStream> GetMemoryStream { get; set; }

        public async Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveFramesAsync(
            Uri requestUri,
            string mediaType,
            string dicomTransferSyntax,
            CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.TryAddWithoutValidation(
                    "Accept",
                    CreateAcceptHeader(CreateMultipartMediaTypeHeader(mediaType), dicomTransferSyntax));

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    return new DicomWebResponse<IReadOnlyList<Stream>>(
                        response,
                        (await ReadMultipartResponseAsStreamsAsync(response.Content, cancellationToken)).ToList());
                }
            }
        }

        public async Task<DicomWebResponse<IReadOnlyList<DicomFile>>> RetrieveInstancesAsync(
            Uri requestUri,
            bool singleInstance,
            string dicomTransferSyntax,
            CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                MediaTypeWithQualityHeaderValue mediaTypeHeader;

                if (singleInstance)
                {
                    mediaTypeHeader = MediaTypeApplicationDicom;
                }
                else
                {
                    mediaTypeHeader = CreateMultipartMediaTypeHeader(ApplicationDicomContentType);
                }

                request.Headers.TryAddWithoutValidation("Accept", CreateAcceptHeader(mediaTypeHeader, dicomTransferSyntax));

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    if (singleInstance)
                    {
                        var memoryStream = GetMemoryStream();
                        await response.Content.CopyToAsync(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        var dicomFile = await DicomFile.OpenAsync(memoryStream);
                        return new DicomWebResponse<IReadOnlyList<DicomFile>>(response, new DicomFile[] { dicomFile });
                    }
                    else
                    {
                        return new DicomWebResponse<IReadOnlyList<DicomFile>>(
                            response,
                            (await ReadMultipartResponseAsStreamsAsync(response.Content, cancellationToken)).Select(x => DicomFile.Open(x)).ToList());
                    }
                }
            }
        }

        public async Task<DicomWebResponse<IReadOnlyList<DicomDataset>>> RetrieveMetadataAsync(Uri requestUri, string ifNoneMatch = null, CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationDicomJson);

                if (!string.IsNullOrEmpty(ifNoneMatch))
                {
                    request.Headers.TryAddWithoutValidation(HeaderNames.IfNoneMatch, ifNoneMatch);
                }

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    await EnsureSuccessOrNotModifiedStatusCodeAsync(response);

                    string contentText = await response.Content.ReadAsStringAsync();

                    return new DicomWebResponse<IReadOnlyList<DicomDataset>>(
                        response,
                        JsonConvert.DeserializeObject<IReadOnlyList<DicomDataset>>(contentText, _jsonSerializerSettings));
                }
            }
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            IEnumerable<DicomFile> dicomFiles,
            string studyInstanceUid = null,
            CancellationToken cancellationToken = default)
        {
            var postContent = new List<Stream>();
            try
            {
                foreach (DicomFile dicomFile in dicomFiles)
                {
                    var stream = GetMemoryStream();
                    await dicomFile.SaveAsync(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    postContent.Add(stream);
                }

                return await PostAsync(postContent, studyInstanceUid, cancellationToken);
            }
            finally
            {
                foreach (var stream in postContent)
                {
                    await stream.DisposeAsync();
                }
            }
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            IEnumerable<Stream> streams,
            string studyInstanceUid = null,
            CancellationToken cancellationToken = default)
        {
            return await PostAsync(streams, studyInstanceUid, cancellationToken);
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            Stream stream,
            string studyInstanceUid = null,
            CancellationToken cancellationToken = default)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return await PostAsync(stream, studyInstanceUid, cancellationToken);
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            DicomFile dicomFile,
            string studyInstanceUid = null,
            CancellationToken cancellationToken = default)
        {
            await using (var stream = GetMemoryStream())
            {
                await dicomFile.SaveAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return await PostAsync(stream, studyInstanceUid, cancellationToken);
            }
        }

        public async Task<DicomWebResponse> DeleteAsync(Uri requestUri, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            using (HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken))
            {
                await EnsureSuccessStatusCodeAsync(response);

                return new DicomWebResponse(response);
            }
        }

        public async Task<DicomWebResponse<IEnumerable<DicomDataset>>> QueryAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationDicomJson);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    var contentText = await response.Content.ReadAsStringAsync();
                    var responseMetadata = JsonConvert.DeserializeObject<IReadOnlyList<DicomDataset>>(contentText, _jsonSerializerSettings);

                    return new DicomWebResponse<IEnumerable<DicomDataset>>(response, responseMetadata);
                }
            }
        }

        public async Task<DicomWebResponse<string>> QueryWithBadRequest(string requestUri, CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationDicomJson);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    var result = new DicomWebResponse<string>(response, await response.Content.ReadAsStringAsync());

                    if (response.IsSuccessStatusCode)
                    {
                        return result;
                    }

                    throw new DicomWebException<string>(result);
                }
            }
        }

        public async Task<DicomWebResponse<IReadOnlyList<ChangeFeedEntry>>> GetChangeFeed(string queryString = "", CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, $"/changefeed{queryString}"))
            {
                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    string contentText = await response.Content.ReadAsStringAsync();

                    return new DicomWebResponse<IReadOnlyList<ChangeFeedEntry>>(
                        response,
                        JsonConvert.DeserializeObject<IReadOnlyList<ChangeFeedEntry>>(contentText, _jsonSerializerSettings));
                }
            }
        }

        public async Task<DicomWebResponse<ChangeFeedEntry>> GetChangeFeedLatest(string queryString = "", CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, $"/changefeed/latest{queryString}"))
            {
                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    string contentText = await response.Content.ReadAsStringAsync();

                    return new DicomWebResponse<ChangeFeedEntry>(
                        response,
                        JsonConvert.DeserializeObject<ChangeFeedEntry>(contentText, _jsonSerializerSettings));
                }
            }
        }

        private static MultipartContent GetMultipartContent(string mimeType)
        {
            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"{mimeType}\""));
            return multiContent;
        }

        private async Task<DicomWebResponse<DicomDataset>> PostAsync(
            IEnumerable<Stream> postContent,
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            MultipartContent multiContent = GetMultipartContent(MediaTypeApplicationDicom.MediaType);

            foreach (Stream content in postContent)
            {
                var streamContent = new StreamContent(content);
                streamContent.Headers.ContentType = MediaTypeApplicationDicom;
                multiContent.Add(streamContent);
            }

            return await PostMultipartContentAsync(
                multiContent,
                string.Format(DicomWebConstants.BaseStudyUriFormat, studyInstanceUid),
                cancellationToken);
        }

        private async Task<DicomWebResponse<DicomDataset>> PostAsync(
            Stream postContent,
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            StreamContent streamContent = new StreamContent(postContent);
            streamContent.Headers.ContentType = MediaTypeApplicationDicom;

            return await PostSinglepartConentAsync(
                streamContent,
                string.Format(DicomWebConstants.BaseStudyUriFormat, studyInstanceUid),
                cancellationToken);
        }

        public async Task<DicomWebResponse<DicomDataset>> PostSinglepartConentAsync(
            StreamContent streamContent,
            string requestUri,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Add(MediaTypeApplicationDicomJson);
            request.Content = streamContent;

            using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                if (response.IsSuccessStatusCode)
                {
                    return await CreateResponseAsync(response);
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    // In the case of Conflict, we will still have body.
                    throw new DicomWebException<DicomDataset>(await CreateResponseAsync(response));
                }
                else
                {
                    throw new DicomWebException(new DicomWebResponse(response));
                }
            }

            async Task<DicomWebResponse<DicomDataset>> CreateResponseAsync(HttpResponseMessage response)
            {
                var contentText = await response.Content.ReadAsStringAsync();

                DicomDataset dataset = JsonConvert.DeserializeObject<DicomDataset>(contentText, _jsonSerializerSettings);

                return new DicomWebResponse<DicomDataset>(response, dataset);
            }
        }

        public async Task<DicomWebResponse<DicomDataset>> PostMultipartContentAsync(
            MultipartContent multiContent,
            string requestUri,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Add(MediaTypeApplicationDicomJson);
            request.Content = multiContent;

            using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                if (response.IsSuccessStatusCode)
                {
                    return await CreateResponseAsync(response);
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    // In the case of Conflict, we will still have body.
                    throw new DicomWebException<DicomDataset>(await CreateResponseAsync(response));
                }
                else
                {
                    throw new DicomWebException(new DicomWebResponse(response));
                }
            }

            async Task<DicomWebResponse<DicomDataset>> CreateResponseAsync(HttpResponseMessage response)
            {
                var contentText = await response.Content.ReadAsStringAsync();

                DicomDataset dataset = JsonConvert.DeserializeObject<DicomDataset>(contentText, _jsonSerializerSettings);

                return new DicomWebResponse<DicomDataset>(response, dataset);
            }
        }

        private async Task<IEnumerable<Stream>> ReadMultipartResponseAsStreamsAsync(HttpContent httpContent, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(httpContent, nameof(httpContent));

            var result = new List<Stream>();
            await using (Stream stream = await httpContent.ReadAsStreamAsync())
            {
                MultipartSection part;
                var media = MediaTypeHeaderValue.Parse(httpContent.Headers.ContentType.ToString());
                var multipartReader = new MultipartReader(HeaderUtilities.RemoveQuotes(media.Boundary).Value, stream, 100);

                while ((part = await multipartReader.ReadNextSectionAsync(cancellationToken)) != null)
                {
                    var memoryStream = GetMemoryStream();
                    await part.Body.CopyToAsync(memoryStream, cancellationToken);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    result.Add(memoryStream);
                }
            }

            return result;
        }

        private static async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                await response.Content.LoadIntoBufferAsync();

                throw new DicomWebException(new DicomWebResponse(response));
            }
        }

        private static async Task EnsureSuccessOrNotModifiedStatusCodeAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotModified)
            {
                await response.Content.LoadIntoBufferAsync();

                throw new DicomWebException(new DicomWebResponse(response));
            }
        }

        private static MediaTypeWithQualityHeaderValue CreateMultipartMediaTypeHeader(string contentType)
        {
            var multipartHeader = new MediaTypeWithQualityHeaderValue(MultipartRelatedContentType);
            var contentHeader = new NameValueHeaderValue("type", "\"" + contentType + "\"");

            multipartHeader.Parameters.Add(contentHeader);
            return multipartHeader;
        }

        private static string CreateAcceptHeader(MediaTypeWithQualityHeaderValue mediaTypeHeader, string dicomTransferSyntax)
        {
            string transferSyntaxHeader = dicomTransferSyntax == null ? string.Empty : $";{TransferSyntaxHeaderName}=\"{dicomTransferSyntax}\"";

            return $"{mediaTypeHeader}{transferSyntaxHeader}";
        }
    }
}
