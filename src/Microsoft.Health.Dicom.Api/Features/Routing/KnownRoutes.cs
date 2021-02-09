// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Api.Features.Routing
{
    public static class KnownRoutes
    {
        private const string StudiesRouteSegment = "studies";
        private const string SeriesRouteSegment = "series";
        private const string InstancesRouteSegment = "instances";
        private const string MetadataSegment = "metadata";
        private const string CustomTagsRouteSegment = "tags";

        private const string StudiesInstanceUidRouteSegment = "{" + KnownActionParameterNames.StudyInstanceUid + "}";
        private const string SeriesInstanceUidRouteSegment = "{" + KnownActionParameterNames.SeriesInstanceUid + "}";
        private const string SopInstanceUidRouteSegment = "{" + KnownActionParameterNames.SopInstanceUid + "}";
        private const string FrameIdsRouteSegment = "{" + KnownActionParameterNames.Frames + "}";

        private const string CustomTagPathRouteSegment = "{" + KnownActionParameterNames.TagPath + "}";

        public const string StoreRoute = StudiesRouteSegment + "/{" + KnownActionParameterNames.StudyInstanceUid + "?}";

        public const string StudyRoute = StudiesRouteSegment + "/" + StudiesInstanceUidRouteSegment;
        public const string SeriesRoute = StudyRoute + "/" + SeriesRouteSegment + "/" + SeriesInstanceUidRouteSegment;
        public const string InstanceRoute = SeriesRoute + "/" + InstancesRouteSegment + "/" + SopInstanceUidRouteSegment;
        public const string FrameRoute = InstanceRoute + "/frames/" + FrameIdsRouteSegment;

        public const string StudyMetadataRoute = StudyRoute + "/" + MetadataSegment;
        public const string SeriesMetadataRoute = SeriesRoute + "/" + MetadataSegment;
        public const string InstanceMetadataRoute = InstanceRoute + "/" + MetadataSegment;

        public const string QueryAllStudiesRoute = StudiesRouteSegment;
        public const string QueryAllSeriesRoute = SeriesRouteSegment;
        public const string QueryAllInstancesRoute = InstancesRouteSegment;
        public const string QuerySeriesInStudyRoute = StudyRoute + "/" + SeriesRouteSegment;
        public const string QueryInstancesInStudyRoute = StudyRoute + "/" + InstancesRouteSegment;
        public const string QueryInstancesInSeriesRoute = SeriesRoute + "/" + InstancesRouteSegment;

        public const string ChangeFeed = "changefeed";
        public const string ChangeFeedLatest = ChangeFeed + "/" + "latest";

        public const string CustomTagRoute = CustomTagsRouteSegment;
        public const string DeleteCustomTagRoute = CustomTagsRouteSegment + "/" + CustomTagPathRouteSegment;
        public const string GetCustomTagRoute = CustomTagsRouteSegment + "/" + CustomTagPathRouteSegment;

        public const string HealthCheck = "/health/check";
    }
}
