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
using Newtonsoft.Json;

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
        internal const string BaseRetrieveInstanceMetadataUriFormat = BaseRetrieveInstanceUriFormat + "/metadata";
        internal const string BaseRetrieveFramesUriFormat = BaseRetrieveInstanceUriFormat + "/frames/{3}";
        private const string TransferSyntaxHeaderName = "transfer-syntax";
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public DicomWebClient(HttpClient httpClient)
        {
            EnsureArg.IsNotNull(httpClient, nameof(httpClient));

            HttpClient = httpClient;
            _jsonSerializerSettings = new JsonSerializerSettings();
            _jsonSerializerSettings.Converters.Add(new JsonDicomConverter(writeTagsAsKeywords: true));
        }

        public HttpClient HttpClient { get; }

        public Task<HttpResult<IReadOnlyList<DicomFile>>> GetStudyAsync(string studyInstanceUID, string dicomTransferSyntax = null)
                => GetInstancesAsync(new Uri(string.Format(BaseRetrieveStudyUriFormat, studyInstanceUID), UriKind.Relative), dicomTransferSyntax);

        public Task<HttpResult<IReadOnlyList<DicomDataset>>> GetStudyMetadataAsync(string studyInstanceUID)
                => GetMetadataAsync(new Uri(string.Format(BaseRetrieveStudyMetadataUriFormat, studyInstanceUID), UriKind.Relative));

        public Task<HttpResult<IReadOnlyList<DicomFile>>> GetSeriesAsync(string studyInstanceUID, string seriesInstanceUID, string dicomTransferSyntax = null)
                => GetInstancesAsync(new Uri(string.Format(BaseRetrieveSeriesUriFormat, studyInstanceUID, seriesInstanceUID), UriKind.Relative), dicomTransferSyntax);

        public Task<HttpResult<IReadOnlyList<DicomDataset>>> GetSeriesMetadataAsync(string studyInstanceUID, string seriesInstanceUID)
                => GetMetadataAsync(new Uri(string.Format(BaseRetrieveSeriesMetadataUriFormat, studyInstanceUID, seriesInstanceUID), UriKind.Relative));

        public Task<HttpResult<IReadOnlyList<DicomFile>>> GetInstanceAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, string dicomTransferSyntax = null)
            => GetInstancesAsync(new Uri(string.Format(BaseRetrieveInstanceUriFormat, studyInstanceUID, seriesInstanceUID, sopInstanceUID), UriKind.Relative), dicomTransferSyntax);

        public Task<HttpResult<IReadOnlyList<DicomDataset>>> GetInstanceMetadataAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
                => GetMetadataAsync(new Uri(string.Format(BaseRetrieveInstanceMetadataUriFormat, studyInstanceUID, seriesInstanceUID, sopInstanceUID), UriKind.Relative));

        public async Task<HttpResult<IReadOnlyList<Stream>>> GetFramesAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, string dicomTransferSyntax = null, params int[] frames)
        {
            var requestUri = new Uri(string.Format(BaseRetrieveFramesUriFormat, studyInstanceUID, seriesInstanceUID, sopInstanceUID, string.Join("%2C", frames)), UriKind.Relative);

            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationOctetStream);
                request.Headers.Add(TransferSyntaxHeaderName, dicomTransferSyntax);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        IEnumerable<Stream> responseStreams = await response.Content.ReadMultipartResponseAsStreamsAsync();
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
                        IEnumerable<Stream> responseStreams = await response.Content.ReadMultipartResponseAsStreamsAsync();
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

        public async Task<HttpResult<DicomDataset>> PostAsync(IEnumerable<DicomFile> dicomFiles, string studyInstanceUID = null)
        {
            var postContent = new List<byte[]>();
            foreach (DicomFile dicomFile in dicomFiles)
            {
                using (var stream = new MemoryStream())
                {
                    await dicomFile.SaveAsync(stream);
                    postContent.Add(stream.ToArray());
                }
            }

            return await PostAsync(postContent, studyInstanceUID);
        }

        public async Task<HttpResult<DicomDataset>> PostAsync(IEnumerable<Stream> streams, string studyInstanceUID = null)
        {
            var postContent = new List<byte[]>();
            foreach (Stream stream in streams)
            {
                byte[] content = await ConvertStreamToByteArrayAsync(stream);
                postContent.Add(content);
            }

            return await PostAsync(postContent, studyInstanceUID);
        }

        private static MultipartContent GetMultipartContent(string mimeType)
        {
            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"{mimeType}\""));
            return multiContent;
        }

        private async Task<HttpResult<DicomDataset>> PostAsync(IEnumerable<byte[]> postContent, string studyInstanceUID)
        {
            MultipartContent multiContent = GetMultipartContent(MediaTypeApplicationDicom.MediaType);

            foreach (byte[] content in postContent)
            {
                var byteContent = new ByteArrayContent(content);
                byteContent.Headers.ContentType = MediaTypeApplicationDicom;
                multiContent.Add(byteContent);
            }

            return await PostMultipartContentAsync(multiContent, $"studies/{studyInstanceUID}");
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
            using (var memory = new MemoryStream())
            {
                await stream.CopyToAsync(memory);
                return memory.ToArray();
            }
        }
    }
}
