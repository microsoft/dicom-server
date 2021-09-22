// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
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
            return new DicomWebResponse<OperationReference>(response, ValueFactory<OperationReference>);
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

        public async Task<DicomWebResponse<IEnumerable<GetExtendedQueryTagEntry>>> GetExtendedQueryTagsAsync(int limit, int offset, CancellationToken cancellationToken)
        {
            EnsureArg.IsGte(limit, 1, nameof(limit));
            EnsureArg.IsGte(offset, 0, nameof(offset));

            var uri = new Uri($"/{_apiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}?{DicomWebConstants.LimitParameter}={limit}&{DicomWebConstants.OffsetParameter}={offset}", UriKind.Relative);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
            return new DicomWebResponse<IEnumerable<GetExtendedQueryTagEntry>>(response, ValueFactory<IEnumerable<GetExtendedQueryTagEntry>>);
        }

        public async Task<DicomWebResponse<GetExtendedQueryTagEntry>> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(tagPath, nameof(tagPath));

            var uri = new Uri($"/{_apiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}/{tagPath}", UriKind.Relative);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
            return new DicomWebResponse<GetExtendedQueryTagEntry>(response, ValueFactory<GetExtendedQueryTagEntry>);
        }

        public async Task<DicomWebResponse<IEnumerable<ExtendedQueryTagError>>> GetExtendedQueryTagErrorsAsync(string tagPath, int limit, int offset, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(tagPath, nameof(tagPath));
            EnsureArg.IsGte(limit, 1, nameof(limit));
            EnsureArg.IsGte(offset, 0, nameof(offset));

            var uri = new Uri(
                string.Format(
                    CultureInfo.InvariantCulture,
                    $"/{_apiVersion}{DicomWebConstants.BaseErrorsUriFormat}?{DicomWebConstants.LimitParameter}={limit}&{DicomWebConstants.OffsetParameter}={offset}",
                    tagPath),
                UriKind.Relative);

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
            return new DicomWebResponse<IEnumerable<ExtendedQueryTagError>>(response, ValueFactory<IEnumerable<ExtendedQueryTagError>>);
        }

        public async Task<DicomWebResponse<GetExtendedQueryTagEntry>> UpdateExtendedQueryTagAsync(string tagPath, UpdateExtendedQueryTagEntry newValue, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(tagPath, nameof(tagPath));
            EnsureArg.IsNotNull(newValue, nameof(newValue));
            EnsureArg.EnumIsDefined(newValue.QueryStatus, nameof(newValue.QueryStatus));
            string jsonString = JsonConvert.SerializeObject(newValue);
            var uri = new Uri($"/{_apiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}", UriKind.Relative);

            using var request = new HttpRequestMessage(HttpMethod.Patch, uri);
            {
                request.Content = new StringContent(jsonString);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
            }

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
            return new DicomWebResponse<GetExtendedQueryTagEntry>(response, ValueFactory<GetExtendedQueryTagEntry>);
        }
    }
}
