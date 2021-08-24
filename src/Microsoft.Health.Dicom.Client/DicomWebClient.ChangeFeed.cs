// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Client.Models;

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

            return new DicomWebResponse<ChangeFeedEntry>(response, ValueFactory<ChangeFeedEntry>);
        }
    }
}
