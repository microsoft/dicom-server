// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.FellowOakDicom.Serialization;

namespace Microsoft.Health.Dicom.Client;

public partial class DicomWebClient : IDicomWebClient
{
    public async Task<DicomWebResponse> AddWorkitemAsync(IEnumerable<DicomDataset> dicomDatasets, string workitemUid, string partitionName, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDatasets, nameof(dicomDatasets));

        var uri = GenerateWorkitemAddRequestUri(workitemUid, partitionName);

        return await Request(uri, dicomDatasets, HttpMethod.Post, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DicomWebResponse> CancelWorkitemAsync(IEnumerable<DicomDataset> dicomDatasets, string workitemUid, string partitionName = default, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomDatasets, nameof(dicomDatasets));

        var uri = GenerateWorkitemCancelRequestUri(workitemUid, partitionName);

        return await Request(uri, dicomDatasets, HttpMethod.Post, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DicomWebResponse<DicomDataset>> RetrieveWorkitemAsync(string workitemUid, string partitionName = default, CancellationToken cancellationToken = default)
    {
        var requestUri = GenerateWorkitemRetrieveRequestUri(workitemUid, partitionName);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);

        var response = await HttpClient.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessStatusCodeAsync(response)
            .ConfigureAwait(false);

        var contentValueFactory = new Func<HttpContent, Task<DicomDataset>>(
            content => ValueFactory<DicomDataset>(content));

        return new DicomWebResponse<DicomDataset>(response, contentValueFactory);
    }

    public async Task<DicomWebResponse> ChangeWorkitemStateAsync(
        DicomDataset dicomDataset,
        string workitemUid,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        var uri = GenerateChangeWorkitemStateRequestUri(workitemUid, partitionName);

        return await Request(uri, dicomDataset, HttpMethod.Put, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryWorkitemAsync(string queryString, string partitionName = default, CancellationToken cancellationToken = default)
    {
        var requestUri = GenerateRequestUri(DicomWebConstants.WorkitemUriString + GetQueryParamUriString(queryString), partitionName);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);

        var response = await HttpClient.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessStatusCodeAsync(response)
            .ConfigureAwait(false);

        return new DicomWebAsyncEnumerableResponse<DicomDataset>(
            response,
            DeserializeAsAsyncEnumerable<DicomDataset>(response.Content));
    }

    private async Task<DicomWebResponse> Request<TContent>(
        Uri uri,
        TContent requestContent,
        HttpMethod httpMethod = null,
        CancellationToken cancellationToken = default) where TContent : class
    {
        EnsureArg.IsNotNull(requestContent, nameof(requestContent));

        JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
        serializerOptions.Converters.Add(new DicomJsonConverter());

        string jsonString = JsonSerializer.Serialize(requestContent, serializerOptions);
        using var request = new HttpRequestMessage(httpMethod ?? HttpMethod.Post, uri);
        {
            request.Content = new StringContent(jsonString);
            request.Content.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicomJson;
        }

        request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);

        var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

        return new DicomWebResponse(response);
    }
}
