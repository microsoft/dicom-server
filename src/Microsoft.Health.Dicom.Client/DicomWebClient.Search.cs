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
        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyAsync(
           string queryString,
           CancellationToken cancellationToken)
        {
            var uri = new Uri("/" + _apiVersion + DicomWebConstants.StudiesUriString + GetQueryParamUriString(queryString), UriKind.Relative);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudySeriesAsync(
            string studyInstanceUid,
            string queryString,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            var uri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.QueryStudySeriesUriFormat, studyInstanceUid) + GetQueryParamUriString(queryString), UriKind.Relative);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyInstanceAsync(
            string studyInstanceUid,
            string queryString,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            var uri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.QueryStudyInstanceUriFormat, studyInstanceUid) + GetQueryParamUriString(queryString), UriKind.Relative);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudySeriesInstanceAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string queryString,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

            var uri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.QueryStudySeriesInstancesUriFormat, studyInstanceUid, seriesInstanceUid) + GetQueryParamUriString(queryString), UriKind.Relative);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesAsync(
            string queryString,
            CancellationToken cancellationToken)
        {
            var uri = new Uri("/" + _apiVersion + DicomWebConstants.SeriesUriString + GetQueryParamUriString(queryString), UriKind.Relative);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesInstanceAsync(
            string seriesInstanceUid,
            string queryString,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

            var uri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.QuerySeriesInstancUriFormat, seriesInstanceUid) + GetQueryParamUriString(queryString), UriKind.Relative);

            return await QueryAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryInstancesAsync(
            string queryString,
            CancellationToken cancellationToken)
        {
            var uri = new Uri("/" + _apiVersion + DicomWebConstants.InstancesUriString + GetQueryParamUriString(queryString), UriKind.Relative);

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
