// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.ModelBinders;
using Microsoft.Health.Dicom.Api.Features.Responses;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers;

[QueryModelStateValidator]
[ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
[ServiceFilter(typeof(PopulateDataPartitionFilterAttribute))]
public class RetrieveController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RetrieveController> _logger;
    private readonly RetrieveConfiguration _retrieveConfiguration;
    private const string IfNoneMatch = "If-None-Match";

    public RetrieveController(IMediator mediator, ILogger<RetrieveController> logger, IOptionsSnapshot<RetrieveConfiguration> retrieveConfiguration)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(retrieveConfiguration?.Value, nameof(retrieveConfiguration));

        _mediator = mediator;
        _logger = logger;
        _retrieveConfiguration = retrieveConfiguration.Value;
    }

    [Produces(KnownContentTypes.MultipartRelated)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
    [HttpGet]
    [VersionedPartitionRoute(KnownRoutes.StudyRoute, Name = KnownRouteNames.PartitionRetrieveStudy)]
    [VersionedRoute(KnownRoutes.StudyRoute, Name = KnownRouteNames.RetrieveStudy)]
    [AuditEventType(AuditEventSubType.Retrieve)]
    public async Task<IActionResult> GetStudyAsync(string studyInstanceUid)
    {
        _logger.LogInformation("DICOM Web Retrieve Transaction request received, for study: {StudyInstanceUid}.", studyInstanceUid);

        RetrieveResourceResponse response = await _mediator.RetrieveDicomStudyAsync(studyInstanceUid, HttpContext.Request.GetAcceptHeaders(), HttpContext.Request.IsOriginalVersionRequested(), HttpContext.RequestAborted);

        return CreateResult(response);
    }

    [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson })]
    [Produces(KnownContentTypes.ApplicationDicomJson)]
    [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
    [ProducesResponseType((int)HttpStatusCode.NotModified)]
    [HttpGet]
    [VersionedPartitionRoute(KnownRoutes.StudyMetadataRoute)]
    [VersionedRoute(KnownRoutes.StudyMetadataRoute)]
    [AuditEventType(AuditEventSubType.RetrieveMetadata)]
    public async Task<IActionResult> GetStudyMetadataAsync([FromHeader(Name = IfNoneMatch)] string ifNoneMatch, string studyInstanceUid)
    {
        _logger.LogInformation("DICOM Web Retrieve Metadata Transaction request received, for study: {StudyInstanceUid}.", studyInstanceUid);

        RetrieveMetadataResponse response = await _mediator.RetrieveDicomStudyMetadataAsync(studyInstanceUid, ifNoneMatch, HttpContext.Request.IsOriginalVersionRequested(), HttpContext.RequestAborted);

        return CreateResult(response);
    }

    [Produces(KnownContentTypes.MultipartRelated)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
    [HttpGet]
    [VersionedPartitionRoute(KnownRoutes.SeriesRoute, Name = KnownRouteNames.PartitionRetrieveSeries)]
    [VersionedRoute(KnownRoutes.SeriesRoute, Name = KnownRouteNames.RetrieveSeries)]
    [AuditEventType(AuditEventSubType.Retrieve)]
    public async Task<IActionResult> GetSeriesAsync(
        string studyInstanceUid,
        string seriesInstanceUid)
    {
        _logger.LogInformation("DICOM Web Retrieve Transaction request received, for study: {StudyInstanceUid}, series: {SeriesInstanceUid}.", studyInstanceUid, seriesInstanceUid);

        RetrieveResourceResponse response = await _mediator.RetrieveDicomSeriesAsync(
            studyInstanceUid, seriesInstanceUid, HttpContext.Request.GetAcceptHeaders(), HttpContext.Request.IsOriginalVersionRequested(), HttpContext.RequestAborted);

        return CreateResult(response);
    }

    [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson })]
    [Produces(KnownContentTypes.ApplicationDicomJson)]
    [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
    [ProducesResponseType((int)HttpStatusCode.NotModified)]
    [HttpGet]
    [VersionedPartitionRoute(KnownRoutes.SeriesMetadataRoute)]
    [VersionedRoute(KnownRoutes.SeriesMetadataRoute)]
    [AuditEventType(AuditEventSubType.RetrieveMetadata)]
    public async Task<IActionResult> GetSeriesMetadataAsync([FromHeader(Name = IfNoneMatch)] string ifNoneMatch, string studyInstanceUid, string seriesInstanceUid)
    {
        _logger.LogInformation("DICOM Web Retrieve Metadata Transaction request received, for study: {StudyInstanceUid}, series: {SeriesInstanceUid}.", studyInstanceUid, seriesInstanceUid);

        RetrieveMetadataResponse response = await _mediator.RetrieveDicomSeriesMetadataAsync(
            studyInstanceUid, seriesInstanceUid, ifNoneMatch, HttpContext.Request.IsOriginalVersionRequested(), HttpContext.RequestAborted);

        return CreateResult(response);
    }

    [Produces(KnownContentTypes.ApplicationDicom, KnownContentTypes.MultipartRelated)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
    [HttpGet]
    [VersionedPartitionRoute(KnownRoutes.InstanceRoute, Name = KnownRouteNames.PartitionRetrieveInstance)]
    [VersionedRoute(KnownRoutes.InstanceRoute, Name = KnownRouteNames.RetrieveInstance)]
    [AuditEventType(AuditEventSubType.Retrieve)]
    public async Task<IActionResult> GetInstanceAsync(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid)
    {
        _logger.LogInformation("DICOM Web Retrieve Transaction request received, for study: '{StudyInstanceUid}', series: '{SeriesInstanceUid}', instance: '{SopInstanceUid}'.", studyInstanceUid, seriesInstanceUid, sopInstanceUid);

        RetrieveResourceResponse response = await _mediator.RetrieveDicomInstanceAsync(
            studyInstanceUid, seriesInstanceUid, sopInstanceUid, HttpContext.Request.GetAcceptHeaders(), HttpContext.Request.IsOriginalVersionRequested(), HttpContext.RequestAborted);

        return CreateResult(response);
    }

    [Produces(KnownContentTypes.ImageJpeg, KnownContentTypes.ImagePng)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
    [HttpGet]
    [VersionedPartitionRoute(KnownRoutes.InstanceRenderedRoute)]
    [VersionedRoute(KnownRoutes.InstanceRenderedRoute)]
    [AuditEventType(AuditEventSubType.RetrieveRendered)]
    public async Task<IActionResult> GetRenderedInstanceAsync(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        [FromQuery] int quality = 100)
    {
        _logger.LogInformation("DICOM Web Retrieve Rendered Image Transaction request for instance received");

        RetrieveRenderedResponse response = await _mediator.RetrieveRenderedDicomInstanceAsync(
            studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Instance, HttpContext.Request.GetAcceptHeaders(), quality, HttpContext.RequestAborted);

        return CreateResult(response);
    }

    [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson })]
    [Produces(KnownContentTypes.ApplicationDicomJson)]
    [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
    [ProducesResponseType((int)HttpStatusCode.NotModified)]
    [HttpGet]
    [VersionedPartitionRoute(KnownRoutes.InstanceMetadataRoute)]
    [VersionedRoute(KnownRoutes.InstanceMetadataRoute)]
    [AuditEventType(AuditEventSubType.RetrieveMetadata)]
    public async Task<IActionResult> GetInstanceMetadataAsync(
        [FromHeader(Name = IfNoneMatch)] string ifNoneMatch,
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid)
    {
        _logger.LogInformation("DICOM Web Retrieve Metadata Transaction request received, for study: {StudyInstanceUid}, series: {SeriesInstanceUid}, instance: {SopInstanceUid}.", studyInstanceUid, seriesInstanceUid, sopInstanceUid);

        RetrieveMetadataResponse response = await _mediator.RetrieveDicomInstanceMetadataAsync(
           studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch, HttpContext.Request.IsOriginalVersionRequested(), HttpContext.RequestAborted);

        return CreateResult(response);
    }

    [Produces(KnownContentTypes.ApplicationDicom, KnownContentTypes.MultipartRelated)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
    [HttpGet]
    [VersionedPartitionRoute(KnownRoutes.FrameRoute, Name = KnownRouteNames.PartitionRetrieveFrame)]
    [VersionedRoute(KnownRoutes.FrameRoute, Name = KnownRouteNames.RetrieveFrame)]
    [AuditEventType(AuditEventSubType.Retrieve)]
    public async Task<IActionResult> GetFramesAsync(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        [FromRoute][ModelBinder(typeof(IntArrayModelBinder))] int[] frames)
    {
        _logger.LogInformation("DICOM Web Retrieve Transaction request received, for study: {StudyInstanceUid}, series: {SeriesInstanceUid}, instance: {SopInstanceUid}, frames: {Frames}.", studyInstanceUid, seriesInstanceUid, sopInstanceUid, string.Join(", ", frames ?? Array.Empty<int>()));
        RetrieveResourceResponse response = await _mediator.RetrieveDicomFramesAsync(
            studyInstanceUid, seriesInstanceUid, sopInstanceUid, frames, HttpContext.Request.GetAcceptHeaders(), HttpContext.RequestAborted);

        return CreateResult(response);
    }

    [Produces(KnownContentTypes.ImageJpeg, KnownContentTypes.ImagePng)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
    [HttpGet]
    [VersionedPartitionRoute(KnownRoutes.FrameRenderedRoute)]
    [VersionedRoute(KnownRoutes.FrameRenderedRoute)]
    [AuditEventType(AuditEventSubType.RetrieveRendered)]
    public async Task<IActionResult> GetRenderedFrameAsync(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        int frame,
        [FromQuery] int quality = 100)
    {
        _logger.LogInformation("DICOM Web Retrieve Rendered Image Transaction request for frame received");

        RetrieveRenderedResponse response = await _mediator.RetrieveRenderedDicomInstanceAsync(
            studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, HttpContext.Request.GetAcceptHeaders(), quality, HttpContext.RequestAborted, frame);

        return CreateResult(response);
    }

    private ResourceResult CreateResult(RetrieveResourceResponse response)
        => new ResourceResult(response, _retrieveConfiguration);

    private static MetadataResult CreateResult(RetrieveMetadataResponse response)
        => new MetadataResult(response);

    private static RenderedResult CreateResult(RetrieveRenderedResponse response)
        => new RenderedResult(response);
}
