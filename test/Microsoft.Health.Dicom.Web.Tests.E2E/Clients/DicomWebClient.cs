// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Clients
{
    public class DicomWebClient
    {
        private readonly HttpClient _httpClient;
        private const string ApplicationDicom = "application/dicom";
        private const string ApplicationDicomJson = "application/dicom+json";

        public DicomWebClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpStatusCode> PostAsync(Stream[] streams, string studyInstanceUID = null)
        {
            MultipartContent multiContent = GetMultipartContent(ApplicationDicom);

            foreach (Stream stream in streams)
            {
                var memory = new MemoryStream();
                await stream.CopyToAsync(memory);
                var byteContent = new ByteArrayContent(memory.ToArray());
                byteContent.Headers.ContentType = new MediaTypeHeaderValue(ApplicationDicom);
                multiContent.Add(byteContent);
            }

            return await PostContentAsync(multiContent, $"studies/{studyInstanceUID}");
        }

        private static MultipartContent GetMultipartContent(string mimeType)
        {
            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", "\"" + mimeType + "\""));
            return multiContent;
        }

        private async Task<HttpStatusCode> PostContentAsync(MultipartContent multiContent, string requestUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(ApplicationDicomJson));
            request.Content = multiContent;

            using (HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                if (response.IsSuccessStatusCode)
                {
                    var contentText = await response.Content.ReadAsStringAsync();
                    Trace.WriteLine(contentText);

                    return response.StatusCode;
                }

                return response.StatusCode;
            }
        }
    }
}
