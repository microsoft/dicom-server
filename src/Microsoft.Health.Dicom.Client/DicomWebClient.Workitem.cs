// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using EnsureThat;
using FellowOakDicom.Serialization;
using System.Text.Json;

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
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(DicomWebConstants.ApplicationJsonMediaType));

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebResponse(response);
        }

        public async Task<DicomWebResponse> CancelWorkitemAsync(DicomDataset dicomDataset, string workitemUid = default, string partitionName = default, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new DicomJsonConverter());

            string jsonString = JsonSerializer.Serialize(dicomDataset, serializerOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, GenerateWorkitemCancelRequestUri(workitemUid, partitionName));
            {
                request.Content = new StringContent(jsonString);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(DicomWebConstants.ApplicationJsonMediaType));

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebResponse(response);
        }
    }
}
