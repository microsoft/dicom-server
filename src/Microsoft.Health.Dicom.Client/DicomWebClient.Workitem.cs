// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Serialization;

namespace Microsoft.Health.Dicom.Client
{
    public partial class DicomWebClient : IDicomWebClient
    {
        public async Task<DicomWebResponse> AddWorkitemAsync(
            IEnumerable<DicomDataset> dicomDatasets,
            string workitemUid,
            string partitionName,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomDatasets, nameof(dicomDatasets));
            JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new DicomJsonConverter());

            string jsonString = JsonSerializer.Serialize(dicomDatasets, serializerOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, GenerateWorkitemAddRequestUri(partitionName, workitemUid));
            {
                request.Content = new StringContent(jsonString);
                request.Content.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicomJson;
            }

            request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebResponse(response);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryWorkitemAsync(string queryString, string partitionName = default, CancellationToken cancellationToken = default)
        {
            var requestUri = GenerateRequestUri(DicomWebConstants.WorkitemUriString + GetQueryParamUriString(queryString), partitionName);

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebAsyncEnumerableResponse<DicomDataset>(
                response,
                DeserializeAsAsyncEnumerable<DicomDataset>(response.Content));
        }
    }
}
