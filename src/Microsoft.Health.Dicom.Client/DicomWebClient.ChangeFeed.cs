// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
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
    public partial class DicomWebClient : IDicomWebClient
    {
        public async Task<DicomWebAsyncEnumerableResponse<ChangeFeedEntry>> GetChangeFeed(string queryString, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri($"/{_apiVersion}/changefeed{queryString}", UriKind.Relative));

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebAsyncEnumerableResponse<ChangeFeedEntry>(
                response,
                DeserializeAsAsyncEnumerable<ChangeFeedEntry>(response.Content));
        }

        public async Task<DicomWebResponse<ChangeFeedEntry>> GetChangeFeedLatest(string queryString, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri($"/{_apiVersion}/changefeed/latest{queryString}", UriKind.Relative));

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebResponse<ChangeFeedEntry>(
                response,
                async content =>
                {
                    string contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<ChangeFeedEntry>(contentText, _jsonSerializerSettings);
                });
        }
    }
}
