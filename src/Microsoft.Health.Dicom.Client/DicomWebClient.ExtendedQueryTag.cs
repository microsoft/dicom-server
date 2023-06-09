// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Client;

public partial class DicomWebClient : IDicomWebClient
{
    public async Task<DicomWebResponse<DicomOperationReference>> AddExtendedQueryTagAsync(IEnumerable<AddExtendedQueryTagEntry> tagEntries, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(tagEntries, nameof(tagEntries));
        string jsonString = JsonSerializer.Serialize(tagEntries, JsonSerializerOptions);
        var uri = new Uri($"/{ApiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}", UriKind.Relative);
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        {
            request.Content = new StringContent(jsonString);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType) { CharSet = Encoding.UTF8.WebName };
        }

        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
        return new DicomWebResponse<DicomOperationReference>(response, ValueFactory<DicomOperationReference>);
    }

    public async Task<DicomWebResponse> DeleteExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNullOrWhiteSpace(tagPath, nameof(tagPath));

        var uri = new Uri($"/{ApiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}/{tagPath}", UriKind.Relative);
        using var request = new HttpRequestMessage(HttpMethod.Delete, uri);

        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

        return new DicomWebResponse(response);
    }

    public async Task<DicomWebResponse<IReadOnlyList<GetExtendedQueryTagEntry>>> GetExtendedQueryTagsAsync(int limit, long offset, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsGte(limit, 1, nameof(limit));
        EnsureArg.IsGte(offset, 0, nameof(offset));

        var uri = new Uri($"/{ApiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}?{DicomWebConstants.LimitParameter}={limit}&{DicomWebConstants.OffsetParameter}={offset}", UriKind.Relative);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
        return new DicomWebResponse<IReadOnlyList<GetExtendedQueryTagEntry>>(response, ValueFactory<IReadOnlyList<GetExtendedQueryTagEntry>>);
    }

    public async Task<DicomWebResponse<GetExtendedQueryTagEntry>> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNullOrWhiteSpace(tagPath, nameof(tagPath));

        var uri = new Uri($"/{ApiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}/{tagPath}", UriKind.Relative);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
        return new DicomWebResponse<GetExtendedQueryTagEntry>(response, ValueFactory<GetExtendedQueryTagEntry>);
    }

    public async Task<DicomWebResponse<IReadOnlyList<ExtendedQueryTagError>>> GetExtendedQueryTagErrorsAsync(string tagPath, int limit, long offset, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNullOrWhiteSpace(tagPath, nameof(tagPath));
        EnsureArg.IsGte(limit, 1, nameof(limit));
        EnsureArg.IsGte(offset, 0, nameof(offset));

        var uri = new Uri(
            string.Format(
                CultureInfo.InvariantCulture,
                $"/{ApiVersion}{DicomWebConstants.BaseErrorsUriFormat}?{DicomWebConstants.LimitParameter}={limit}&{DicomWebConstants.OffsetParameter}={offset}",
                tagPath),
            UriKind.Relative);

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
        return new DicomWebResponse<IReadOnlyList<ExtendedQueryTagError>>(response, ValueFactory<IReadOnlyList<ExtendedQueryTagError>>);
    }

    public async Task<DicomWebResponse<GetExtendedQueryTagEntry>> UpdateExtendedQueryTagAsync(string tagPath, UpdateExtendedQueryTagEntry newValue, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNullOrWhiteSpace(tagPath, nameof(tagPath));
        EnsureArg.IsNotNull(newValue, nameof(newValue));
        EnsureArg.EnumIsDefined(newValue.QueryStatus, nameof(newValue));
        string jsonString = JsonSerializer.Serialize(newValue, JsonSerializerOptions);
        var uri = new Uri($"/{ApiVersion}{DicomWebConstants.BaseExtendedQueryTagUri}/{tagPath}", UriKind.Relative);

        using var request = new HttpRequestMessage(
#if NETSTANDARD2_0
            new HttpMethod("PATCH"),
#else
            HttpMethod.Patch,
#endif
            uri);
        {
            request.Content = new StringContent(jsonString);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType) { CharSet = Encoding.UTF8.WebName };
        }

        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
        return new DicomWebResponse<GetExtendedQueryTagEntry>(response, ValueFactory<GetExtendedQueryTagEntry>);
    }
}
