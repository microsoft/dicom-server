// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Clients
{
    public static class DicomWebClientExtensions
    {
        public static Task<DicomWebResponse<IReadOnlyList<DicomFile>>> RetrieveStudyAsync(this DicomWebClient dicomWebClient, string studyInstanceUid, string dicomTransferSyntax = null)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return dicomWebClient.RetrieveInstancesAsync(
                new Uri(string.Format(DicomWebContants.BaseRetrieveStudyUriFormat, studyInstanceUid), UriKind.Relative),
                dicomTransferSyntax);
        }

        public static Task<DicomWebResponse<IReadOnlyList<DicomDataset>>> RetrieveStudyMetadataAsync(this DicomWebClient dicomWebClient, string studyInstanceUid)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return dicomWebClient.RetrieveMetadataAsync(
                new Uri(string.Format(DicomWebContants.BaseRetrieveStudyMetadataUriFormat, studyInstanceUid), UriKind.Relative));
        }

        public static Task<DicomWebResponse<IReadOnlyList<DicomFile>>> RetrieveSeriesAsync(this DicomWebClient dicomWebClient, string studyInstanceUid, string seriesInstanceUid, string dicomTransferSyntax = null)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return dicomWebClient.RetrieveInstancesAsync(
                new Uri(string.Format(DicomWebContants.BaseRetrieveSeriesUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative),
                dicomTransferSyntax);
        }

        public static Task<DicomWebResponse<IReadOnlyList<DicomDataset>>> RetrieveSeriesMetadataAsync(this DicomWebClient dicomWebClient, string studyInstanceUid, string seriesInstanceUid)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return dicomWebClient.RetrieveMetadataAsync(
                new Uri(string.Format(DicomWebContants.BaseRetrieveSeriesMetadataUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative));
        }

        public static Task<DicomWebResponse<IReadOnlyList<DicomFile>>> RetrieveInstanceAsync(this DicomWebClient dicomWebClient, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string dicomTransferSyntax = null)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return dicomWebClient.RetrieveInstancesAsync(
                new Uri(string.Format(DicomWebContants.BaseRetrieveInstanceUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative),
                dicomTransferSyntax);
        }

        public static Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveInstanceRenderedAsync(this DicomWebClient dicomWebClient, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUId, string format = null, bool thumbnail = false)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            string urlFormat = thumbnail ? DicomWebContants.BaseRetrieveInstanceThumbnailUriFormat : DicomWebContants.BaseRetrieveInstanceRenderedUriFormat;

            return dicomWebClient.RetrieveInstancesRenderedAsync(
                new Uri(string.Format(urlFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUId), UriKind.Relative),
                format,
                thumbnail);
        }

        public static Task<DicomWebResponse<IReadOnlyList<DicomDataset>>> RetrieveInstanceMetadataAsync(this DicomWebClient dicomWebClient, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            return dicomWebClient.RetrieveMetadataAsync(
                new Uri(string.Format(DicomWebContants.BaseRetrieveInstanceMetadataUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative));
        }

        public static Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveFramesRenderedAsync(this DicomWebClient dicomWebClient, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string format = null, bool thumbnail = false, params int[] frames)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            var uriString = thumbnail ? DicomWebContants.BaseRetrieveFramesThumbnailUriFormat : DicomWebContants.BaseRetrieveFramesRenderedUriFormat;

            var requestUri = new Uri(string.Format(uriString, studyInstanceUid, seriesInstanceUid, sopInstanceUid, string.Join(",", frames)), UriKind.Relative);

            return dicomWebClient.RetrieveFramesRenderedAsync(requestUri, format);
        }

        public static Task<DicomWebResponse<IReadOnlyList<Stream>>> RetrieveFramesAsync(this DicomWebClient dicomWebClient, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string dicomTransferSyntax = null, params int[] frames)
        {
            var requestUri = new Uri(string.Format(DicomWebContants.BaseRetrieveFramesUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid, string.Join("%2C", frames)), UriKind.Relative);

            return dicomWebClient.RetrieveFramesAsync(requestUri, dicomTransferSyntax);
        }

        public static Task<DicomWebResponse> DeleteStudyAsync(this DicomWebClient dicomWebClient, string studyInstanceUid)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            var requestUri = new Uri(string.Format(DicomWebContants.BaseRetrieveStudyUriFormat, studyInstanceUid), UriKind.Relative);

            return dicomWebClient.DeleteAsync(requestUri);
        }

        public static Task<DicomWebResponse> DeleteSeriesAsync(this DicomWebClient dicomWebClient, string studyInstanceUid, string seriesInstanceUid)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            var requestUri = new Uri(string.Format(DicomWebContants.BaseRetrieveSeriesUriFormat, studyInstanceUid, seriesInstanceUid), UriKind.Relative);

            return dicomWebClient.DeleteAsync(requestUri);
        }

        public static Task<DicomWebResponse> DeleteInstanceAsync(this DicomWebClient dicomWebClient, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            var requestUri = new Uri(string.Format(DicomWebContants.BaseRetrieveInstanceUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), UriKind.Relative);

            return dicomWebClient.DeleteAsync(requestUri);
        }
    }
}
