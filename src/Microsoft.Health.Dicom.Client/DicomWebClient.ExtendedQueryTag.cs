// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Client
{
    public partial class DicomWebClient : IDicomWebClient
    {
        public async Task<DicomWebResponse<OperationReference>> AddExtendedQueryTagAsync(IEnumerable<AddExtendedQueryTagEntry> tagEntries, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(tagEntries, nameof(tagEntries));
            string jsonString = JsonConvert.SerializeObject(tagEntries);
            var uri = new Uri($"/{_apiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}", UriKind.Relative);
            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            {
                request.Content = new StringContent(jsonString);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
            }

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
            return new DicomWebResponse<OperationReference>(
                response,
                async content =>
                {
                    string text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<OperationReference>(text, _jsonSerializerSettings);
                }
            );
        }

        public async Task<DicomWebResponse> DeleteExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(tagPath, nameof(tagPath));

            var uri = new Uri($"/{_apiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}/{tagPath}", UriKind.Relative);
            using var request = new HttpRequestMessage(HttpMethod.Delete, uri);

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebResponse(response);
        }

        public async Task<DicomWebResponse<IEnumerable<GetExtendedQueryTagEntry>>> GetExtendedQueryTagsAsync(CancellationToken cancellationToken)
        {
            var uri = new Uri($"/{_apiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}", UriKind.Relative);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
            return new DicomWebResponse<IEnumerable<GetExtendedQueryTagEntry>>(
                 response,
                 async content =>
                 {
                     string contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                     return JsonConvert.DeserializeObject<IEnumerable<GetExtendedQueryTagEntry>>(contentText, _jsonSerializerSettings);
                 });
        }

        public async Task<DicomWebResponse<GetExtendedQueryTagEntry>> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            var uri = new Uri($"/{_apiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}/{tagPath}", UriKind.Relative);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
            return new DicomWebResponse<GetExtendedQueryTagEntry>(
                 response,
                 async content =>
                 {
                     string contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                     return JsonConvert.DeserializeObject<GetExtendedQueryTagEntry>(contentText, _jsonSerializerSettings);
                 });
        }
    }
}
