// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client
{
    public partial class DicomWebClient : IDicomWebClient
    {
        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyAsync(
           string queryString,
           string partitionName = default,
           CancellationToken cancellationToken = default)
        {
            var uri = GenerateRequestUri(DicomWebConstants.StudiesUriString + GetQueryParamUriString(queryString), partitionName);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudySeriesAsync(
            string studyInstanceUid,
            string queryString,
            string partitionName = default,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            var uri = GenerateRequestUri(string.Format(DicomWebConstants.QueryStudySeriesUriFormat, studyInstanceUid) + GetQueryParamUriString(queryString), partitionName);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyInstanceAsync(
            string studyInstanceUid,
            string queryString,
            string partitionName = default,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            var uri = GenerateRequestUri(string.Format(DicomWebConstants.QueryStudyInstanceUriFormat, studyInstanceUid) + GetQueryParamUriString(queryString), partitionName);

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

            var uri = GenerateRequestUri(string.Format(DicomWebConstants.QueryStudySeriesInstancesUriFormat, studyInstanceUid, seriesInstanceUid) + GetQueryParamUriString(queryString), partitionName);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesAsync(
            string queryString,
            string partitionName = default,
            CancellationToken cancellationToken = default)
        {
            var uri = GenerateRequestUri(DicomWebConstants.SeriesUriString + GetQueryParamUriString(queryString), partitionName);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesInstanceAsync(
            string seriesInstanceUid,
            string queryString,
            string partitionName = default,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

            var uri = GenerateRequestUri(string.Format(DicomWebConstants.QuerySeriesInstanceUriFormat, seriesInstanceUid) + GetQueryParamUriString(queryString), partitionName);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryInstancesAsync(
            string queryString,
            string partitionName = default,
            CancellationToken cancellationToken = default)
        {
            var uri = GenerateRequestUri(DicomWebConstants.InstancesUriString + GetQueryParamUriString(queryString), partitionName);

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
}
