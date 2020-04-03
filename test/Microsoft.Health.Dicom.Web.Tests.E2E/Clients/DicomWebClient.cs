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
        internal const string BaseRetrieveStudyUriFormat = "/studies/{0}";
        internal const string BaseRetrieveStudyMetadataUriFormat = BaseRetrieveStudyUriFormat + "/metadata";
        internal const string BaseRetrieveSeriesUriFormat = BaseRetrieveStudyUriFormat + "/series/{1}";
        internal const string BaseRetrieveSeriesMetadataUriFormat = BaseRetrieveSeriesUriFormat + "/metadata";
        internal const string BaseRetrieveInstanceUriFormat = BaseRetrieveSeriesUriFormat + "/instances/{2}";
        internal const string BaseRetrieveInstanceRenderedUriFormat = BaseRetrieveInstanceUriFormat + "/rendered";
        internal const string BaseRetrieveInstanceThumbnailUriFormat = BaseRetrieveInstanceUriFormat + "/thumbnail";
        internal const string BaseRetrieveInstanceMetadataUriFormat = BaseRetrieveInstanceUriFormat + "/metadata";
        internal const string BaseRetrieveFramesUriFormat = BaseRetrieveInstanceUriFormat + "/frames/{3}";
        internal const string BaseRetrieveFramesRenderedUriFormat = BaseRetrieveFramesUriFormat + "/rendered";
        internal const string BaseRetrieveFramesThumbnailUriFormat = BaseRetrieveFramesUriFormat + "/thumbnail";
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

        public Task<HttpResult<IReadOnlyList<DicomFile>>> GetStudyAsync(string studyInstanceUid, string dicomTransferSyntax = null)
                => GetInstancesAsync(new Uri(string.Format(BaseRetrieveStudyUriFormat, studyInstanceUid), UriKind.Relative), dicomTransferSyntax);

        public Task<HttpResult<IReadOnlyList<DicomDataset>>> GetStudyMetadataAsync(string studyInstanceUid)
                => GetMetadataAsync(new Uri(string.Format(BaseRetrieveStudyMetadataUriFormat, studyInstanceUid), UriKind.Relative));

        public Task<HttpResult<IReadOnlyList<DicomFile>>> GetSeriesAsync(string studyInstanceUid, string seriesInstanceUid, string dicomTransferSyntax = null)
                => GetInstancesAsync(new Uri(string.Format(BaseRetrieveSeriesUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative), dicomTransferSyntax);

        public Task<HttpResult<IReadOnlyList<DicomDataset>>> GetSeriesMetadataAsync(string studyInstanceUid, string seriesInstanceUid)
                => GetMetadataAsync(new Uri(string.Format(BaseRetrieveSeriesMetadataUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative));

        public Task<HttpResult<IReadOnlyList<DicomFile>>> GetInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string dicomTransferSyntax = null)
            => GetInstancesAsync(new Uri(string.Format(BaseRetrieveInstanceUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative), dicomTransferSyntax);

        public Task<HttpResult<IReadOnlyList<Stream>>> GetInstanceRenderedAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUId, string format = null, bool thumbnail = false)
            => GetInstancesRenderedAsync(new Uri(string.Format(thumbnail ? BaseRetrieveInstanceThumbnailUriFormat : BaseRetrieveInstanceRenderedUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUId), UriKind.Relative), format, thumbnail);

        public Task<HttpResult<IReadOnlyList<DicomDataset>>> GetInstanceMetadataAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
                => GetMetadataAsync(new Uri(string.Format(BaseRetrieveInstanceMetadataUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative));

        public async Task<HttpResult<IReadOnlyList<Stream>>> GetFramesRenderedAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string format = null, bool thumbnail = false, params int[] frames)
        {
            var uriString = thumbnail ? BaseRetrieveFramesThumbnailUriFormat : BaseRetrieveFramesRenderedUriFormat;

            var requestUri = new Uri(string.Format(uriString, studyInstanceUid, seriesInstanceUid, sopInstanceUid, string.Join(",", frames)), UriKind.Relative);

            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(format));

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        IEnumerable<Stream> responseStreams = await ReadMultipartResponseAsStreamsAsync(response.Content);
                        return new HttpResult<IReadOnlyList<Stream>>(response.StatusCode, responseStreams.ToList());
                    }

                    return new HttpResult<IReadOnlyList<Stream>>(response.StatusCode);
                }
            }
        }

        public async Task<HttpResult<IReadOnlyList<Stream>>> GetFramesAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string dicomTransferSyntax = null, params int[] frames)
        {
            var requestUri = new Uri(string.Format(BaseRetrieveFramesUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid, string.Join("%2C", frames)), UriKind.Relative);

            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationOctetStream);
                request.Headers.Add(TransferSyntaxHeaderName, dicomTransferSyntax);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        IEnumerable<Stream> responseStreams = await ReadMultipartResponseAsStreamsAsync(response.Content);
                        return new HttpResult<IReadOnlyList<Stream>>(response.StatusCode, responseStreams.ToList());
                    }

                    return new HttpResult<IReadOnlyList<Stream>>(response.StatusCode);
                }
            }
        }

        public async Task<HttpResult<IReadOnlyList<Stream>>> GetInstancesRenderedAsync(Uri requestUri, string format = null, bool thumbnail = false)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(format));

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        IEnumerable<Stream> responseStreams = await ReadMultipartResponseAsStreamsAsync(response.Content);
                        return new HttpResult<IReadOnlyList<Stream>>(response.StatusCode, responseStreams.ToList());
                    }

                    return new HttpResult<IReadOnlyList<Stream>>(response.StatusCode);
                }
            }
        }

        public async Task<HttpResult<IReadOnlyList<DicomFile>>> GetInstancesAsync(Uri requestUri, string dicomTransferSyntax = null)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationDicom);
                request.Headers.Add(TransferSyntaxHeaderName, dicomTransferSyntax);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        IEnumerable<Stream> responseStreams = await ReadMultipartResponseAsStreamsAsync(response.Content);
                        return new HttpResult<IReadOnlyList<DicomFile>>(response.StatusCode, responseStreams.Select(x => DicomFile.Open(x)).ToList());
                    }

                    return new HttpResult<IReadOnlyList<DicomFile>>(response.StatusCode);
                }
            }
        }

        public async Task<HttpResult<IReadOnlyList<DicomDataset>>> GetMetadataAsync(Uri requestUri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationDicomJson);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var contentText = await response.Content.ReadAsStringAsync();
                        IReadOnlyList<DicomDataset> responseMetadata = JsonConvert.DeserializeObject<IReadOnlyList<DicomDataset>>(contentText, _jsonSerializerSettings);

                        return new HttpResult<IReadOnlyList<DicomDataset>>(response.StatusCode, responseMetadata);
                    }

                    return new HttpResult<IReadOnlyList<DicomDataset>>(response.StatusCode);
                }
            }
        }

        public async Task<HttpResult<DicomDataset>> PostAsync(IEnumerable<DicomFile> dicomFiles, string studyInstanceUid = null)
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

        public async Task<HttpResult<DicomDataset>> PostAsync(IEnumerable<Stream> streams, string studyInstanceUid = null)
        {
            var postContent = new List<byte[]>();
            foreach (Stream stream in streams)
            {
                byte[] content = await ConvertStreamToByteArrayAsync(stream);
                postContent.Add(content);
            }

            return await PostAsync(postContent, studyInstanceUid);
        }

        public async Task<HttpStatusCode> DeleteAsync(string studyInstanceUid, string seriesInstanceUid = null, string sopInstanceUid = null)
        {
            string url = string.IsNullOrEmpty(seriesInstanceUid) ? $"studies/{studyInstanceUid}"
                : string.IsNullOrEmpty(sopInstanceUid) ? $"studies/{studyInstanceUid}/series/{seriesInstanceUid}"
                : $"studies/{studyInstanceUid}/series/{seriesInstanceUid}/instances/{sopInstanceUid}";

            var request = new HttpRequestMessage(HttpMethod.Delete, url);

            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                return response.StatusCode;
            }
        }

        public async Task<HttpResult<IEnumerable<DicomDataset>>> QueryAsync(string requestUri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationDicomJson);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var contentText = await response.Content.ReadAsStringAsync();
                        var responseMetadata = JsonConvert.DeserializeObject<IReadOnlyList<DicomDataset>>(contentText, _jsonSerializerSettings);

                        return new HttpResult<IEnumerable<DicomDataset>>(response.StatusCode, responseMetadata);
                    }

                    return new HttpResult<IEnumerable<DicomDataset>>(response.StatusCode);
                }
            }
        }

        public async Task<HttpResult<string>> QueryWithBadRequest(string requestUri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationDicomJson);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    return new HttpResult<string>(response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
        }

        private static MultipartContent GetMultipartContent(string mimeType)
        {
            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"{mimeType}\""));
            return multiContent;
        }

        private async Task<HttpResult<DicomDataset>> PostAsync(IEnumerable<byte[]> postContent, string studyInstanceUid)
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

        internal async Task<HttpResult<DicomDataset>> PostMultipartContentAsync(MultipartContent multiContent, string requestUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Add(MediaTypeApplicationDicomJson);
            request.Content = multiContent;

            using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                if (response.StatusCode == HttpStatusCode.OK ||
                       response.StatusCode == HttpStatusCode.Accepted ||
                       response.StatusCode == HttpStatusCode.Conflict)
                {
                    var contentText = await response.Content.ReadAsStringAsync();
                    DicomDataset dataset = JsonConvert.DeserializeObject<DicomDataset>(contentText, _jsonSerializerSettings);

                    return new HttpResult<DicomDataset>(response.StatusCode, dataset);
                }

                return new HttpResult<DicomDataset>(response.StatusCode);
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
    }
}
