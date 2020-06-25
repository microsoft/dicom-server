// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Tools.ScaleTesting.Common
{
    public class DicomWebConstants
    {
        public const string BaseStudyUriFormat = "/studies/{0}";
        public const string BaseRetrieveStudyMetadataUriFormat = BaseStudyUriFormat + "/metadata";
        public const string BaseSeriesUriFormat = BaseStudyUriFormat + "/series/{1}";
        public const string BaseRetrieveSeriesMetadataUriFormat = BaseSeriesUriFormat + "/metadata";
        public const string BaseInstanceUriFormat = BaseSeriesUriFormat + "/instances/{2}";
        public const string BaseRetrieveInstanceRenderedUriFormat = BaseInstanceUriFormat + "/rendered";
        public const string BaseRetrieveInstanceThumbnailUriFormat = BaseInstanceUriFormat + "/thumbnail";
        public const string BaseRetrieveInstanceMetadataUriFormat = BaseInstanceUriFormat + "/metadata";
        public const string BaseRetrieveFramesUriFormat = BaseInstanceUriFormat + "/frames/{3}";
        public const string BaseRetrieveFramesRenderedUriFormat = BaseRetrieveFramesUriFormat + "/rendered";
        public const string BaseRetrieveFramesThumbnailUriFormat = BaseRetrieveFramesUriFormat + "/thumbnail";
    }
}
