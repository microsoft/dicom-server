// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Dicom.Serialization;
using EnsureThat;
using Newtonsoft.Json;

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

            string jsonString = JsonConvert.SerializeObject(dicomDatasets, new JsonDicomConverter());
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
    }
}
