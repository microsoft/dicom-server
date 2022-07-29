// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Client;

public partial class DicomWebClient : IDicomWebClient
{
    public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyAsync(
       string queryString,
       string partitionName = default,
       CancellationToken cancellationToken = default)
    {
        var uri = GenerateRequestUri(DicomWebConstants.StudiesUriString + FormatQueryString(queryString), partitionName);

        return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    public Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudySeriesAsync(
        string studyInstanceUid,
        string queryString,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

        var uri = GenerateRequestUri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.QueryStudySeriesUriFormat, studyInstanceUid) + FormatQueryString(queryString), partitionName);

        return QueryAsync(uri, cancellationToken);
    }

    public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyInstanceAsync(
        string studyInstanceUid,
        string queryString,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

        var uri = GenerateRequestUri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.QueryStudyInstanceUriFormat, studyInstanceUid) + FormatQueryString(queryString), partitionName);

        return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudySeriesInstanceAsync(
        string studyInstanceUid,
        string seriesInstanceUid,
        string queryString,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
        EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

        var uri = GenerateRequestUri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.QueryStudySeriesInstancesUriFormat, studyInstanceUid, seriesInstanceUid) + FormatQueryString(queryString), partitionName);

        return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesAsync(
        string queryString,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        var uri = GenerateRequestUri(DicomWebConstants.SeriesUriString + FormatQueryString(queryString), partitionName);

        return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesInstanceAsync(
        string seriesInstanceUid,
        string queryString,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

        var uri = GenerateRequestUri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.QuerySeriesInstanceUriFormat, seriesInstanceUid) + FormatQueryString(queryString), partitionName);

        return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryInstancesAsync(
        string queryString,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        var uri = GenerateRequestUri(DicomWebConstants.InstancesUriString + FormatQueryString(queryString), partitionName);

        return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryAsync(
        Uri requestUri,
        CancellationToken cancellationToken)
    {
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
