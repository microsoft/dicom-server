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

        public const string ApplicationDicomMeidaType = "application/dicom";
        public const string ApplicationDicomJsonMeidaType = "application/dicom+json";
        public const string ApplicationOctetStreamMeidaType = "application/octet-stream";
        public const string ApplicationJsonMeidaType = "application/json";
        public const string ImageJpegMeidaType = "image/jpeg";
        public const string ImagePngMeidaType = "image/png";
        public const string MultipartRelatedMeidaType = "multipart/related";
        public const string ImageJpeg2000MeidaType = "image/jp2";
        public const string ImageDicomRleMeidaType = "image/dicom-rle";
        public const string ImageJpegLsMeidaType = "image/jls";
        public const string ImageJpeg2000Part2MeidaType = "image/jpx";
        public const string VideoMpeg2MeidaType = "video/mpeg2";
        public const string VideoMp4MeidaType = "video/mp4";
    }
}
