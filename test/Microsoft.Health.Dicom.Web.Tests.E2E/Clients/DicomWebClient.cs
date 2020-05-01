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
using IdentityModel.Client;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
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
        private readonly (bool Enabled, string TokenUrl) _securitySettings;
        private readonly Dictionary<string, string> _bearerTokens = new Dictionary<string, string>();

        public DicomWebClient(
            HttpClient httpClient,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager,
            TestApplication testApplication,
            (bool enabled, string tokenUrl) securitySettings)
        {
            HttpClient = httpClient;
            _jsonSerializerSettings = new JsonSerializerSettings();
            _jsonSerializerSettings.Converters.Add(new JsonDicomConverter(writeTagsAsKeywords: true));
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
            _securitySettings = securitySettings;
            SetupAuthenticationAsync(HttpClient, testApplication).GetAwaiter().GetResult();
        }

        public HttpClient HttpClient { get; }

        public bool SecurityEnabled => _securitySettings.Enabled;

        public async Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveFramesRenderedAsync(
            Uri requestUri,
            string format = null,
            CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(format));

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    return new DicomWebResponse<IReadOnlyList<Stream>>(
                        response,
                        (await ReadMultipartResponseAsStreamsAsync(response.Content, cancellationToken)).ToList());
                }
            }
        }

        public async Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveFramesAsync(
            Uri requestUri,
            string dicomTransferSyntax = null,
            string expectedContentTypeHeader = null,
            CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationOctetStream);
                request.Headers.Add(TransferSyntaxHeaderName, dicomTransferSyntax);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    return new DicomWebResponse<IReadOnlyList<Stream>>(
                        response,
                        (await ReadMultipartResponseAsStreamsAsync(response.Content, cancellationToken, expectedContentTypeHeader)).ToList());
                }
            }
        }

        public async Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveInstancesRenderedAsync(
            Uri requestUri,
            string format = null,
            CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(format));

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
            string dicomTransferSyntax = null,
            CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationDicom);
                request.Headers.Add(TransferSyntaxHeaderName, dicomTransferSyntax);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    return new DicomWebResponse<IReadOnlyList<DicomFile>>(
                        response,
                        (await ReadMultipartResponseAsStreamsAsync(response.Content, cancellationToken)).Select(x => DicomFile.Open(x)).ToList());
                }
            }
        }

        public async Task<DicomWebResponse<IReadOnlyList<DicomDataset>>> RetrieveMetadataAsync(Uri requestUri, CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(MediaTypeApplicationDicomJson);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    await EnsureSuccessStatusCodeAsync(response);

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
            var postContent = new List<byte[]>();

            foreach (DicomFile dicomFile in dicomFiles)
            {
                await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
                {
                    await dicomFile.SaveAsync(stream);
                    postContent.Add(stream.ToArray());
                }
            }

            return await PostAsync(postContent, studyInstanceUid, cancellationToken);
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            IEnumerable<Stream> streams,
            string studyInstanceUid = null,
            CancellationToken cancellationToken = default)
        {
            var postContent = new List<byte[]>();

            foreach (Stream stream in streams)
            {
                byte[] content = await ConvertStreamToByteArrayAsync(stream, cancellationToken);
                postContent.Add(content);
            }

            return await PostAsync(postContent, studyInstanceUid, cancellationToken);
        }

        public async Task<DicomWebResponse> DeleteAsync(Uri requestUri, CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Delete, requestUri))
            {
                using (HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken))
                {
                    await EnsureSuccessStatusCodeAsync(response);

                    return new DicomWebResponse(response);
                }
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

        private static MultipartContent GetMultipartContent(string mimeType)
        {
            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"{mimeType}\""));
            return multiContent;
        }

        private async Task<DicomWebResponse<DicomDataset>> PostAsync(
            IEnumerable<byte[]> postContent,
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            MultipartContent multiContent = GetMultipartContent(MediaTypeApplicationDicom.MediaType);

            foreach (byte[] content in postContent)
            {
                var byteContent = new ByteArrayContent(content);
                byteContent.Headers.ContentType = MediaTypeApplicationDicom;
                multiContent.Add(byteContent);
            }

            return await PostMultipartContentAsync(
                multiContent,
                string.Format(DicomWebConstants.BasStudyUriFormat, studyInstanceUid),
                cancellationToken);
        }

        internal async Task<DicomWebResponse<DicomDataset>> PostMultipartContentAsync(
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

        private async Task<byte[]> ConvertStreamToByteArrayAsync(Stream stream, CancellationToken cancellationToken)
        {
            await using (MemoryStream memory = _recyclableMemoryStreamManager.GetStream())
            {
                await stream.CopyToAsync(memory, cancellationToken);
                return memory.ToArray();
            }
        }

        private async Task<IEnumerable<Stream>> ReadMultipartResponseAsStreamsAsync(HttpContent httpContent, CancellationToken cancellationToken, string expectedContentTypeHeader = null)
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
                    if (!string.IsNullOrEmpty(expectedContentTypeHeader))
                    {
                        Assert.Equal(expectedContentTypeHeader, part.ContentType);
                    }

                    MemoryStream memoryStream = _recyclableMemoryStreamManager.GetStream();
                    await part.Body.CopyToAsync(memoryStream, cancellationToken);
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

        private async Task SetupAuthenticationAsync(HttpClient httpClient, TestApplication clientApplication, TestUser user = null)
        {
            if (_securitySettings.Enabled)
            {
                var tokenKey = $"{clientApplication.ClientId}:{(user == null ? string.Empty : user.UserId)}";

                if (!_bearerTokens.TryGetValue(tokenKey, out string bearerToken))
                {
                    bearerToken = await GetBearerToken(clientApplication, user, _securitySettings.TokenUrl);
                    _bearerTokens[tokenKey] = bearerToken;
                }

                httpClient.SetBearerToken(bearerToken);
            }
        }

        private async Task<string> GetBearerToken(TestApplication clientApplication, TestUser user, string tokenUrl)
        {
            if (clientApplication.Equals(TestApplications.InvalidClient))
            {
                return null;
            }

            var formContent = new FormUrlEncodedContent(GetAppSecuritySettings(clientApplication));

            HttpResponseMessage tokenResponse = await HttpClient.PostAsync(tokenUrl, formContent);

            var tokenJson = JObject.Parse(await tokenResponse.Content.ReadAsStringAsync());

            var bearerToken = tokenJson["access_token"].Value<string>();

            return bearerToken;
        }

        private List<KeyValuePair<string, string>> GetAppSecuritySettings(TestApplication clientApplication)
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", clientApplication.ClientId),
                new KeyValuePair<string, string>("client_secret", clientApplication.ClientSecret),
                new KeyValuePair<string, string>("grant_type", clientApplication.GrantType),
                new KeyValuePair<string, string>("scope", AuthenticationSettings.Scope),
                new KeyValuePair<string, string>("resource", AuthenticationSettings.Resource),
            };
        }
    }
}
