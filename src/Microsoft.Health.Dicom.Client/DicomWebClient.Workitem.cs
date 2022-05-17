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

        return await PostRequest(uri, dicomDatasets, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DicomWebResponse> CancelWorkitemAsync(IEnumerable<DicomDataset> dicomDatasets, string workitemUid, string partitionName = default, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomDatasets, nameof(dicomDatasets));

        var uri = GenerateWorkitemCancelRequestUri(workitemUid, partitionName);

        return await PostRequest(uri, dicomDatasets, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DicomWebResponse<DicomDataset>> RetrieveWorkitemAsync(string workitemUid, string partitionName = default, CancellationToken cancellationToken = default)
    {
        var requestUri = GenerateWorkitemRetrieveRequestUri(workitemUid, partitionName);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);

        var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

        var contentValueFactory = new Func<HttpContent, Task<DicomDataset>>(
            content => Deserialize<DicomDataset>(content));

        return new DicomWebResponse<DicomDataset>(response, contentValueFactory);
    }

    public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryWorkitemAsync(string queryString, string partitionName = default, CancellationToken cancellationToken = default)
    {
        var requestUri = GenerateRequestUri(DicomWebConstants.WorkitemUriString + GetQueryParamUriString(queryString), partitionName);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);

        var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

        return new DicomWebAsyncEnumerableResponse<DicomDataset>(
            response,
            DeserializeAsAsyncEnumerable<DicomDataset>(response.Content));
    }

    public async Task<DicomWebResponse> UpdateWorkitemAsync(DicomDataset dicomDataset, string workitemUid, string transactionUid, string partitionName, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemUid, nameof(workitemUid));
        EnsureArg.IsNotEmptyOrWhiteSpace(transactionUid, nameof(transactionUid));

        var uri = GenerateWorkitemUpdateRequestUri(workitemUid, transactionUid, partitionName);

        return await PostRequest(uri, dicomDataset, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DicomWebResponse> PostRequest<TContent>(
        Uri uri,
        TContent requestContent,
        CancellationToken cancellationToken = default) where TContent : class
    {
        EnsureArg.IsNotNull(requestContent, nameof(requestContent));

        JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
        serializerOptions.Converters.Add(new DicomJsonConverter());

        string jsonString = JsonSerializer.Serialize(requestContent, serializerOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
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
