// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Api.Features.Routing;

public static class KnownRoutes
{
    private const string PartitionsRouteSegment = "partitions";
    private const string StudiesRouteSegment = "studies";
    private const string SeriesRouteSegment = "series";
    private const string InstancesRouteSegment = "instances";
    private const string MetadataSegment = "metadata";
    private const string RenderedSegment = "rendered";
    private const string ErrorsSegment = "errors";
    private const string ExtendedQueryTagsRouteSegment = "extendedquerytags";
    private const string OperationsSegment = "operations";
    private const string WorkitemsRouteSegment = "workitems";
    private const string WorkitemCancelRequest = "cancelrequest";
    private const string ExportSegment = "export";

    private const string PartitionNameRouteSegment = "{" + KnownActionParameterNames.PartitionName + "}";
    private const string StudiesInstanceUidRouteSegment = "{" + KnownActionParameterNames.StudyInstanceUid + "}";
    private const string SeriesInstanceUidRouteSegment = "{" + KnownActionParameterNames.SeriesInstanceUid + "}";
    private const string SopInstanceUidRouteSegment = "{" + KnownActionParameterNames.SopInstanceUid + "}";
    private const string FrameIdsRouteSegment = "{" + KnownActionParameterNames.Frames + "}";
    private const string SingleFrameIdRouteSegment = "{" + KnownActionParameterNames.Frame + "}";

    private const string ExtendedQueryTagPathRouteSegment = "{" + KnownActionParameterNames.TagPath + "}";

    public const string GetAllPartitionsRoute = PartitionsRouteSegment;
    public const string StoreInstancesRoute = StudiesRouteSegment;
    public const string StoreInstancesInStudyRoute = StudiesRouteSegment + "/{" + KnownActionParameterNames.StudyInstanceUid + "}";

    public const string AddWorkitemInstancesRoute = WorkitemsRouteSegment;
    public const string RetrieveWorkitemInstancesRoute = WorkitemsRouteSegment + "/{" + KnownActionParameterNames.WorkItemInstanceUid + "}";
    public const string UpdateWorkitemInstancesRoute = WorkitemsRouteSegment + "/{" + KnownActionParameterNames.WorkItemInstanceUid + "}";
    public const string SearchWorkitemInstancesRoute = WorkitemsRouteSegment;
    public const string CancelWorkitemInstancesRoute = WorkitemsRouteSegment + "/{" + KnownActionParameterNames.WorkItemInstanceUid + "}/" + WorkitemCancelRequest;
    public const string ChangeStateWorkitemInstancesRoute = WorkitemsRouteSegment + "/{" + KnownActionParameterNames.WorkItemInstanceUid + "}/state";

    public const string PartitionRoute = PartitionsRouteSegment + "/" + PartitionNameRouteSegment;
    public const string StudyRoute = StudiesRouteSegment + "/" + StudiesInstanceUidRouteSegment;
    public const string SeriesRoute = StudyRoute + "/" + SeriesRouteSegment + "/" + SeriesInstanceUidRouteSegment;
    public const string InstanceRoute = SeriesRoute + "/" + InstancesRouteSegment + "/" + SopInstanceUidRouteSegment;
    public const string FrameRoute = InstanceRoute + "/frames/" + FrameIdsRouteSegment;

    public const string StudyMetadataRoute = StudyRoute + "/" + MetadataSegment;
    public const string SeriesMetadataRoute = SeriesRoute + "/" + MetadataSegment;
    public const string InstanceMetadataRoute = InstanceRoute + "/" + MetadataSegment;

    public const string InstanceRenderedRoute = InstanceRoute + "/" + RenderedSegment;
    public const string FrameRenderedRoute = InstanceRoute + "/frames/" + SingleFrameIdRouteSegment + "/" + RenderedSegment;

    public const string QueryAllStudiesRoute = StudiesRouteSegment;
    public const string QueryAllSeriesRoute = SeriesRouteSegment;
    public const string QueryAllInstancesRoute = InstancesRouteSegment;
    public const string QuerySeriesInStudyRoute = StudyRoute + "/" + SeriesRouteSegment;
    public const string QueryInstancesInStudyRoute = StudyRoute + "/" + InstancesRouteSegment;
    public const string QueryInstancesInSeriesRoute = SeriesRoute + "/" + InstancesRouteSegment;

    public const string ExportInstancesRoute = ExportSegment;

    public const string ChangeFeed = "changefeed";
    public const string ChangeFeedLatest = ChangeFeed + "/" + "latest";

    public const string ExtendedQueryTagRoute = ExtendedQueryTagsRouteSegment;
    public const string DeleteExtendedQueryTagRoute = ExtendedQueryTagsRouteSegment + "/" + ExtendedQueryTagPathRouteSegment;
    public const string GetExtendedQueryTagRoute = ExtendedQueryTagsRouteSegment + "/" + ExtendedQueryTagPathRouteSegment;
    public const string GetExtendedQueryTagErrorsRoute = GetExtendedQueryTagRoute + "/" + ErrorsSegment;
    public const string UpdateExtendedQueryTagQueryStatusRoute = GetExtendedQueryTagRoute;

    public const string HealthCheck = "/health/check";

    public const string OperationInstanceRoute = OperationsSegment + "/{" + KnownActionParameterNames.OperationId + "}";

    public const string UpdateInstanceRoute = StudiesRouteSegment + "/$bulkUpdate";
}
