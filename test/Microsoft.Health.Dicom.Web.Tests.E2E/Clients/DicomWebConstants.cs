// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Clients
{
    public class DicomWebConstants
    {
        public const string BasStudyUriFormat = "/studies/{0}";
        public const string BaseRetrieveStudyMetadataUriFormat = BasStudyUriFormat + "/metadata";
        public const string BaseSeriesUriFormat = BasStudyUriFormat + "/series/{1}";
        public const string BaseRetrieveSeriesMetadataUriFormat = BaseSeriesUriFormat + "/metadata";
        public const string BaseeInstanceUriFormat = BaseSeriesUriFormat + "/instances/{2}";
        public const string BaseRetrieveInstanceRenderedUriFormat = BaseeInstanceUriFormat + "/rendered";
        public const string BaseRetrieveInstanceThumbnailUriFormat = BaseeInstanceUriFormat + "/thumbnail";
        public const string BaseRetrieveInstanceMetadataUriFormat = BaseeInstanceUriFormat + "/metadata";
        public const string BaseRetrieveFramesUriFormat = BaseeInstanceUriFormat + "/frames/{3}";
        public const string BaseRetrieveFramesRenderedUriFormat = BaseRetrieveFramesUriFormat + "/rendered";
        public const string BaseRetrieveFramesThumbnailUriFormat = BaseRetrieveFramesUriFormat + "/thumbnail";
    }
}
