// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client
{
    public static class DicomWebClientExtensions
    {
        public static Task<DicomWebResponse<IReadOnlyList<DicomFile>>> RetrieveStudyAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return dicomWebClient.RetrieveInstancesAsync(
                new Uri(string.Format(DicomWebConstants.BasStudyUriFormat, studyInstanceUid), UriKind.Relative),
                false,
                dicomTransferSyntax,
                cancellationToken);
        }

        public static Task<DicomWebResponse<IReadOnlyList<DicomDataset>>> RetrieveStudyMetadataAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return dicomWebClient.RetrieveMetadataAsync(
                new Uri(string.Format(DicomWebConstants.BaseRetrieveStudyMetadataUriFormat, studyInstanceUid), UriKind.Relative),
                cancellationToken);
        }

        public static Task<DicomWebResponse<IReadOnlyList<DicomFile>>> RetrieveSeriesAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return dicomWebClient.RetrieveInstancesAsync(
                new Uri(string.Format(DicomWebConstants.BaseSeriesUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative),
                false,
                dicomTransferSyntax,
                cancellationToken);
        }

        public static Task<DicomWebResponse<IReadOnlyList<DicomDataset>>> RetrieveSeriesMetadataAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return dicomWebClient.RetrieveMetadataAsync(
                new Uri(string.Format(DicomWebConstants.BaseRetrieveSeriesMetadataUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative),
                cancellationToken);
        }

        public static Task<DicomWebResponse<IReadOnlyList<DicomFile>>> RetrieveInstanceAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return dicomWebClient.RetrieveInstancesAsync(
                new Uri(string.Format(DicomWebConstants.BaseInstanceUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative),
                true,
                dicomTransferSyntax,
                cancellationToken);
        }

        public static Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveInstanceRenderedAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUId,
            string format = null,
            bool thumbnail = false,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            string urlFormat = thumbnail ? DicomWebConstants.BaseRetrieveInstanceThumbnailUriFormat : DicomWebConstants.BaseRetrieveInstanceRenderedUriFormat;

            return dicomWebClient.RetrieveInstancesRenderedAsync(
                new Uri(string.Format(urlFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUId), UriKind.Relative),
                format,
                cancellationToken);
        }

        public static Task<DicomWebResponse<IReadOnlyList<DicomDataset>>> RetrieveInstanceMetadataAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return dicomWebClient.RetrieveMetadataAsync(
                new Uri(string.Format(DicomWebConstants.BaseRetrieveInstanceMetadataUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative),
                cancellationToken);
        }

        public static Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveFramesRenderedAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string format = null,
            bool thumbnail = false,
            int[] frames = null,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            var uriString = thumbnail ? DicomWebConstants.BaseRetrieveFramesThumbnailUriFormat : DicomWebConstants.BaseRetrieveFramesRenderedUriFormat;

            var requestUri = new Uri(string.Format(uriString, studyInstanceUid, seriesInstanceUid, sopInstanceUid, string.Join(",", frames)), UriKind.Relative);

            return dicomWebClient.RetrieveFramesRenderedAsync(requestUri, format, cancellationToken);
        }

        public static Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveFramesAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string dicomTransferSyntax = DicomWebConstants.OriginalDicomTransferSyntax,
            int[] frames = null,
            CancellationToken cancellationToken = default)
        {
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseRetrieveFramesUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid, string.Join("%2C", frames)), UriKind.Relative);

            return dicomWebClient.RetrieveFramesAsync(requestUri, dicomTransferSyntax, cancellationToken);
        }

        public static Task<DicomWebResponse> DeleteStudyAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            var requestUri = new Uri(string.Format(DicomWebConstants.BasStudyUriFormat, studyInstanceUid), UriKind.Relative);

            return dicomWebClient.DeleteAsync(requestUri, cancellationToken);
        }

        public static Task<DicomWebResponse> DeleteSeriesAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            var requestUri = new Uri(string.Format(DicomWebConstants.BaseSeriesUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative);

            return dicomWebClient.DeleteAsync(requestUri, cancellationToken);
        }

        public static Task<DicomWebResponse> DeleteInstanceAsync(
            this IDicomWebClient dicomWebClient,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            var requestUri = new Uri(string.Format(DicomWebConstants.BaseInstanceUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative);

            return dicomWebClient.DeleteAsync(requestUri, cancellationToken);
        }
    }
}
