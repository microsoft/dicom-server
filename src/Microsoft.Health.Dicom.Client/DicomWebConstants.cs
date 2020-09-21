// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Client
{
    public static class DicomWebConstants
    {
        public const string BasStudyUriFormat = "/studies/{0}";
        public const string BaseRetrieveStudyMetadataUriFormat = BasStudyUriFormat + "/metadata";
        public const string BaseSeriesUriFormat = BasStudyUriFormat + "/series/{1}";
        public const string BaseRetrieveSeriesMetadataUriFormat = BaseSeriesUriFormat + "/metadata";
        public const string BaseInstanceUriFormat = BaseSeriesUriFormat + "/instances/{2}";
        public const string BaseRetrieveInstanceRenderedUriFormat = BaseInstanceUriFormat + "/rendered";
        public const string BaseRetrieveInstanceThumbnailUriFormat = BaseInstanceUriFormat + "/thumbnail";
        public const string BaseRetrieveInstanceMetadataUriFormat = BaseInstanceUriFormat + "/metadata";
        public const string BaseRetrieveFramesUriFormat = BaseInstanceUriFormat + "/frames/{3}";
        public const string BaseRetrieveFramesRenderedUriFormat = BaseRetrieveFramesUriFormat + "/rendered";
        public const string BaseRetrieveFramesThumbnailUriFormat = BaseRetrieveFramesUriFormat + "/thumbnail";
        public const string OriginalDicomTransferSyntax = "*";

        public const string ApplicationDicomMediaType = "application/dicom";
        public const string ApplicationDicomJsonMediaType = "application/dicom+json";
        public const string ApplicationOctetStreamMediaType = "application/octet-stream";
        public const string ApplicationJsonMediaType = "application/json";
        public const string ImageJpegMediaType = "image/jpeg";
        public const string ImagePngMediaType = "image/png";
        public const string MultipartRelatedMediaType = "multipart/related";
        public const string ImageJpeg2000MediaType = "image/jp2";
        public const string ImageDicomRleMediaType = "image/dicom-rle";
        public const string ImageJpegLsMediaType = "image/jls";
        public const string ImageJpeg2000Part2MediaType = "image/jpx";
        public const string VideoMpeg2MediaType = "video/mpeg2";
        public const string VideoMp4MediaType = "video/mp4";
    }
}
