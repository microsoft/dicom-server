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
using System.Threading.Tasks;
using Dicom;
using Dicom.Serialization;
using EnsureThat;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using MediaTypeHeaderValue = Microsoft.Net.Http.Headers.MediaTypeHeaderValue;
using NameValueHeaderValue = System.Net.Http.Headers.NameValueHeaderValue;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Clients
{
    public class DicomWebClient
    {
        public static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationDicom = new MediaTypeWithQualityHeaderValue("application/dicom");
        public static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationOctetStream = new MediaTypeWithQualityHeaderValue("application/octet-stream");
        public static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationDicomJson = new MediaTypeWithQualityHeaderValue("application/dicom+json");

        private const string TransferSyntaxHeaderName = "transfer-syntax";
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public DicomWebClient(HttpClient httpClient, RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            EnsureArg.IsNotNull(httpClient, nameof(httpClient));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

            HttpClient = httpClient;
            _jsonSerializerSettings = new JsonSerializerSettings();
            _jsonSerializerSettings.Converters.Add(new JsonDicomConverter(writeTagsAsKeywords: true));

            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        public HttpClient HttpClient { get; }

        public async Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveFramesRenderedAsync(Uri requestUri, string format = null)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(format));

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    return new DicomWebResponse<IReadOnlyList<Stream>>(
                        response,
                        (await ReadMultipartResponseAsStreamsAsync(response.Content)).ToList());
                }
            }
        }

        public async Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveFramesAsync(Uri requestUri, string dicomTransferSyntax = null)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationOctetStream);
                request.Headers.Add(TransferSyntaxHeaderName, dicomTransferSyntax);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    return new DicomWebResponse<IReadOnlyList<Stream>>(
                        response,
                        (await ReadMultipartResponseAsStreamsAsync(response.Content)).ToList());
                }
            }
        }

        public async Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveInstancesRenderedAsync(Uri requestUri, string format = null, bool thumbnail = false)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(format));

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    return new DicomWebResponse<IReadOnlyList<Stream>>(
                        response,
                        (await ReadMultipartResponseAsStreamsAsync(response.Content)).ToList());
                }
            }
        }

        public async Task<DicomWebResponse<IReadOnlyList<DicomFile>>> RetrieveInstancesAsync(Uri requestUri, string dicomTransferSyntax = null)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationDicom);
                request.Headers.Add(TransferSyntaxHeaderName, dicomTransferSyntax);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    return new DicomWebResponse<IReadOnlyList<DicomFile>>(
                        response,
                        (await ReadMultipartResponseAsStreamsAsync(response.Content)).Select(x => DicomFile.Open(x)).ToList());
                }
            }
        }

        public async Task<DicomWebResponse<IReadOnlyList<DicomDataset>>> RetrieveMetadataAsync(Uri requestUri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationDicomJson);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    string contentText = await response.Content.ReadAsStringAsync();

                    return new DicomWebResponse<IReadOnlyList<DicomDataset>>(
                        response,
                        JsonConvert.DeserializeObject<IReadOnlyList<DicomDataset>>(contentText, _jsonSerializerSettings));
                }
            }
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(IEnumerable<DicomFile> dicomFiles, string studyInstanceUid = null)
        {
            var postContent = new List<byte[]>();

            foreach (DicomFile dicomFile in dicomFiles)
            {
                await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
                {
                    await dicomFile.SaveAsync(stream);
                    postContent.Add(stream.ToArray());
                }
            }

            return await PostAsync(postContent, studyInstanceUid);
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(IEnumerable<Stream> streams, string studyInstanceUid = null)
        {
            var postContent = new List<byte[]>();

            foreach (Stream stream in streams)
            {
                byte[] content = await ConvertStreamToByteArrayAsync(stream);
                postContent.Add(content);
            }

            return await PostAsync(postContent, studyInstanceUid);
        }

        public async Task<DicomWebResponse> DeleteAsync(Uri requestUri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Delete, requestUri))
            {
                using (HttpResponseMessage response = await HttpClient.SendAsync(request))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    return new DicomWebResponse(response);
                }
            }
        }

        public async Task<DicomWebResponse<IEnumerable<DicomDataset>>> QueryAsync(string requestUri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationDicomJson);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    var contentText = await response.Content.ReadAsStringAsync();
                    var responseMetadata = JsonConvert.DeserializeObject<IReadOnlyList<DicomDataset>>(contentText, _jsonSerializerSettings);

                    return new DicomWebResponse<IEnumerable<DicomDataset>>(response, responseMetadata);
                }
            }
        }

        public async Task<DicomWebResponse<string>> QueryWithBadRequest(string requestUri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationDicomJson);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
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

        private static MultipartContent GetMultipartContent(string mimeType)
        {
            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"{mimeType}\""));
            return multiContent;
        }

        private async Task<DicomWebResponse<DicomDataset>> PostAsync(IEnumerable<byte[]> postContent, string studyInstanceUid)
        {
            MultipartContent multiContent = GetMultipartContent(MediaTypeApplicationDicom.MediaType);

            foreach (byte[] content in postContent)
            {
                var byteContent = new ByteArrayContent(content);
                byteContent.Headers.ContentType = MediaTypeApplicationDicom;
                multiContent.Add(byteContent);
            }

            return await PostMultipartContentAsync(multiContent, $"studies/{studyInstanceUid}");
        }

        internal async Task<DicomWebResponse<DicomDataset>> PostMultipartContentAsync(MultipartContent multiContent, string requestUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Add(MediaTypeApplicationDicomJson);
            request.Content = multiContent;

            using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
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

        private async Task<byte[]> ConvertStreamToByteArrayAsync(Stream stream)
        {
            await using (MemoryStream memory = _recyclableMemoryStreamManager.GetStream())
            {
                await stream.CopyToAsync(memory);
                return memory.ToArray();
            }
        }

        private async Task<IEnumerable<Stream>> ReadMultipartResponseAsStreamsAsync(HttpContent httpContent)
        {
            EnsureArg.IsNotNull(httpContent, nameof(httpContent));

            var result = new List<Stream>();
            await using (Stream stream = await httpContent.ReadAsStreamAsync())
            {
                MultipartSection part;
                var media = MediaTypeHeaderValue.Parse(httpContent.Headers.ContentType.ToString());
                var multipartReader = new MultipartReader(HeaderUtilities.RemoveQuotes(media.Boundary).Value, stream, 100);

                while ((part = await multipartReader.ReadNextSectionAsync()) != null)
                {
                    MemoryStream memoryStream = _recyclableMemoryStreamManager.GetStream();
                    await part.Body.CopyToAsync(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    result.Add(memoryStream);
                }
            }

            return result;
        }

        private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                await response.Content.LoadIntoBufferAsync();

                throw new DicomWebException(new DicomWebResponse(response));
            }
        }
    }
}
