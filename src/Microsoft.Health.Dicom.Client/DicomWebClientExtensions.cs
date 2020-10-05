// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client
{
    public static class DicomWebClientExtensions
    {
        public static async Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveStudyAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return await dicomWebClient.RetrieveInstancesAsync(
                new Uri(string.Format(DicomWebConstants.BaseStudyUriFormat, studyInstanceUid), UriKind.Relative),
                dicomTransferSyntax,
                cancellationToken).ConfigureAwait(false);
        }

        public static async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveStudyMetadataAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string ifNoneMatch = null,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return await dicomWebClient.RetrieveMetadataAsync(
                new Uri(string.Format(DicomWebConstants.BaseRetrieveStudyMetadataUriFormat, studyInstanceUid), UriKind.Relative),
                ifNoneMatch,
                cancellationToken).ConfigureAwait(false);
        }

        public static async Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveSeriesAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return await dicomWebClient.RetrieveInstancesAsync(
                new Uri(string.Format(DicomWebConstants.BaseSeriesUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative),
                dicomTransferSyntax,
                cancellationToken).ConfigureAwait(false);
        }

        public static async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveSeriesMetadataAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            string ifNoneMatch = null,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return await dicomWebClient.RetrieveMetadataAsync(
                new Uri(string.Format(DicomWebConstants.BaseRetrieveSeriesMetadataUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative),
                ifNoneMatch,
                cancellationToken).ConfigureAwait(false);
        }

        public static async Task<DicomWebResponse<DicomFile>> RetrieveInstanceAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return await dicomWebClient.RetrieveInstanceAsync(
                new Uri(string.Format(DicomWebConstants.BaseInstanceUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative),
                dicomTransferSyntax,
                cancellationToken).ConfigureAwait(false);
        }

        public static async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveInstanceMetadataAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string ifNoneMatch = null,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return await dicomWebClient.RetrieveMetadataAsync(
                new Uri(string.Format(DicomWebConstants.BaseRetrieveInstanceMetadataUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative),
                ifNoneMatch,
                cancellationToken).ConfigureAwait(false);
        }

        public static async Task<DicomWebAsyncEnumerableResponse<Stream>> RetrieveFramesAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string mediaType = DicomWebConstants.ApplicationOctetStreamMediaType,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            int[] frames = null,
            CancellationToken cancellationToken = default)
        {
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseRetrieveFramesUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid, string.Join("%2C", frames)), UriKind.Relative);

            return await dicomWebClient.RetrieveFramesAsync(requestUri, mediaType, dicomTransferSyntax, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<DicomWebResponse> DeleteStudyAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            var requestUri = new Uri(string.Format(DicomWebConstants.BaseStudyUriFormat, studyInstanceUid), UriKind.Relative);

            return await dicomWebClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<DicomWebResponse> DeleteSeriesAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            var requestUri = new Uri(string.Format(DicomWebConstants.BaseSeriesUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative);

            return await dicomWebClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<DicomWebResponse> DeleteInstanceAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            var requestUri = new Uri(string.Format(DicomWebConstants.BaseInstanceUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative);

            return await dicomWebClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        }
    }
}
