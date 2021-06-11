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
        private const string ExtendedQueryTagsRouteSegment = "extendedquerytags";
        private const string VersionSegment = "v{version:apiVersion}";

        private const string StudiesInstanceUidRouteSegment = "{" + KnownActionParameterNames.StudyInstanceUid + "}";
        private const string SeriesInstanceUidRouteSegment = "{" + KnownActionParameterNames.SeriesInstanceUid + "}";
        private const string SopInstanceUidRouteSegment = "{" + KnownActionParameterNames.SopInstanceUid + "}";
        private const string FrameIdsRouteSegment = "{" + KnownActionParameterNames.Frames + "}";

        private const string ExtendedQueryTagPathRouteSegment = "{" + KnownActionParameterNames.TagPath + "}";

        public const string StoreRoute = StudiesRouteSegment + "/{" + KnownActionParameterNames.StudyInstanceUid + "?}";
        public const string VersionedStoreRoute = VersionSegment + "/" + StoreRoute;

        public const string StudyRoute = StudiesRouteSegment + "/" + StudiesInstanceUidRouteSegment;
        public const string VersionedStudyRoute = VersionSegment + "/" + StudyRoute;
        public const string SeriesRoute = StudyRoute + "/" + SeriesRouteSegment + "/" + SeriesInstanceUidRouteSegment;
        public const string VersionedSeriesRoute = VersionSegment + "/" + SeriesRoute;
        public const string InstanceRoute = SeriesRoute + "/" + InstancesRouteSegment + "/" + SopInstanceUidRouteSegment;
        public const string VersionedInstanceRoute = VersionSegment + "/" + InstanceRoute;
        public const string FrameRoute = InstanceRoute + "/frames/" + FrameIdsRouteSegment;
        public const string VersionedFrameRoute = VersionSegment + "/" + FrameRoute;

        public const string StudyMetadataRoute = StudyRoute + "/" + MetadataSegment;
        public const string VersionedStudyMetadataRoute = VersionSegment + "/" + StudyMetadataRoute;
        public const string SeriesMetadataRoute = SeriesRoute + "/" + MetadataSegment;
        public const string VersionedSeriesMetadataRoute = VersionSegment + "/" + SeriesMetadataRoute;
        public const string InstanceMetadataRoute = InstanceRoute + "/" + MetadataSegment;
        public const string VersionedInstanceMetadataRoute = VersionSegment + "/" + InstanceMetadataRoute;

        public const string QueryAllStudiesRoute = StudiesRouteSegment;
        public const string VersionedQueryAllStudiesRoute = VersionSegment + "/" + QueryAllStudiesRoute;
        public const string QueryAllSeriesRoute = SeriesRouteSegment;
        public const string VersionedQueryAllSeriesRoute = VersionSegment + "/" + QueryAllSeriesRoute;
        public const string QueryAllInstancesRoute = InstancesRouteSegment;
        public const string VersionedQueryAllInstancesRoute = VersionSegment + "/" + QueryAllInstancesRoute;
        public const string QuerySeriesInStudyRoute = StudyRoute + "/" + SeriesRouteSegment;
        public const string VersionedQuerySeriesInStudyRoute = VersionSegment + "/" + QuerySeriesInStudyRoute;
        public const string QueryInstancesInStudyRoute = StudyRoute + "/" + InstancesRouteSegment;
        public const string VersionedQueryInstancesInStudyRoute = VersionSegment + "/" + QueryInstancesInStudyRoute;
        public const string QueryInstancesInSeriesRoute = SeriesRoute + "/" + InstancesRouteSegment;
        public const string VersionedQueryInstancesInSeriesRoute = VersionSegment + "/" + QueryInstancesInSeriesRoute;

        public const string ChangeFeed = "changefeed";
        public const string VersionedChangeFeed = VersionSegment + "/" + ChangeFeed;
        public const string ChangeFeedLatest = ChangeFeed + "/" + "latest";
        public const string VersionedChangeFeedLatest = VersionSegment + "/" + ChangeFeedLatest;

        public const string ExtendedQueryTagRoute = ExtendedQueryTagsRouteSegment;
        public const string VersionedExtendedQueryTagRoute = VersionSegment + "/" + ExtendedQueryTagRoute;
        public const string DeleteExtendedQueryTagRoute = ExtendedQueryTagsRouteSegment + "/" + ExtendedQueryTagPathRouteSegment;
        public const string VersionedDeleteExtendedQueryTagRoute = VersionSegment + "/" + DeleteExtendedQueryTagRoute;
        public const string GetExtendedQueryTagRoute = ExtendedQueryTagsRouteSegment + "/" + ExtendedQueryTagPathRouteSegment;
        public const string VersionedGetExtendedQueryTagRoute = VersionSegment + "/" + GetExtendedQueryTagRoute;

        public const string HealthCheck = "/health/check";
    }
}
