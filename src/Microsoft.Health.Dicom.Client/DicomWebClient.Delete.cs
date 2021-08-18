// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client
{
    public partial class DicomWebClient : IDicomWebClient
    {
        public async Task<DicomWebResponse> DeleteStudyAsync(
            string studyInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            var requestUri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseStudyUriFormat, studyInstanceUid), UriKind.Relative);

            return await DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebResponse> DeleteSeriesAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

            var requestUri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseSeriesUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative);

            return await DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DicomWebResponse> DeleteInstanceAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));

            var requestUri = new Uri("/" + _apiVersion + string.Format(DicomWebConstants.BaseInstanceUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative);

            return await DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        }

        private async Task<DicomWebResponse> DeleteAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebResponse(response);
        }
    }
}
