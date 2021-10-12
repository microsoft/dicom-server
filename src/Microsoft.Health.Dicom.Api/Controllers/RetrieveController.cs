// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.ModelBinders;
using Microsoft.Health.Dicom.Api.Features.Responses;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ApiVersion("1.0-prerelease")]
    [QueryModelStateValidator]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    public class RetrieveController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RetrieveController> _logger;
        private const string IfNoneMatch = "If-None-Match";

        public RetrieveController(IMediator mediator, ILogger<RetrieveController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [Produces(KnownContentTypes.MultipartRelated)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [VersionedRoute(KnownRoutes.StudyRoute, Name = KnownRouteNames.VersionedRetrieveStudy)]
        [Route(KnownRoutes.StudyRoute, Name = KnownRouteNames.RetrieveStudy)]
        [AuditEventType(AuditEventSubType.Retrieve)]
        public async Task<IActionResult> GetStudyAsync(string studyInstanceUid)
        {
            _logger.LogInformation("DICOM Web Retrieve Transaction request received, for study: {studyInstanceUid}.", studyInstanceUid);

            RetrieveResourceResponse response = await _mediator.RetrieveDicomStudyAsync(studyInstanceUid, HttpContext.Request.GetAcceptHeaders(), HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType((int)HttpStatusCode.NotModified)]
        [HttpGet]
        [VersionedRoute(KnownRoutes.StudyMetadataRoute)]
        [Route(KnownRoutes.StudyMetadataRoute)]
        [AuditEventType(AuditEventSubType.RetrieveMetadata)]
        public async Task<IActionResult> GetStudyMetadataAsync([FromHeader(Name = IfNoneMatch)] string ifNoneMatch, string studyInstanceUid)
        {
            _logger.LogInformation("DICOM Web Retrieve Metadata Transaction request received, for study: {studyInstanceUid}.", studyInstanceUid);

            RetrieveMetadataResponse response = await _mediator.RetrieveDicomStudyMetadataAsync(studyInstanceUid, ifNoneMatch, HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [Produces(KnownContentTypes.MultipartRelated)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [VersionedRoute(KnownRoutes.SeriesRoute, Name = KnownRouteNames.VersionedRetrieveSeries)]
        [Route(KnownRoutes.SeriesRoute, Name = KnownRouteNames.RetrieveSeries)]
        [AuditEventType(AuditEventSubType.Retrieve)]
        public async Task<IActionResult> GetSeriesAsync(
            string studyInstanceUid,
            string seriesInstanceUid)
        {
            _logger.LogInformation("DICOM Web Retrieve Transaction request received, for study: {studyInstanceUid}, series: {seriesInstanceUid}.", studyInstanceUid, seriesInstanceUid);

            RetrieveResourceResponse response = await _mediator.RetrieveDicomSeriesAsync(
                studyInstanceUid, seriesInstanceUid, HttpContext.Request.GetAcceptHeaders(), HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType((int)HttpStatusCode.NotModified)]
        [HttpGet]
        [VersionedRoute(KnownRoutes.SeriesMetadataRoute)]
        [Route(KnownRoutes.SeriesMetadataRoute)]
        [AuditEventType(AuditEventSubType.RetrieveMetadata)]
        public async Task<IActionResult> GetSeriesMetadataAsync([FromHeader(Name = IfNoneMatch)] string ifNoneMatch, string studyInstanceUid, string seriesInstanceUid)
        {
            _logger.LogInformation("DICOM Web Retrieve Metadata Transaction request received, for study: {studyInstanceUid}, series: {seriesInstanceUid}.", studyInstanceUid, seriesInstanceUid);

            RetrieveMetadataResponse response = await _mediator.RetrieveDicomSeriesMetadataAsync(
                studyInstanceUid, seriesInstanceUid, ifNoneMatch, HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [Produces(KnownContentTypes.ApplicationDicom, KnownContentTypes.MultipartRelated)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [VersionedRoute(KnownRoutes.InstanceRoute, Name = KnownRouteNames.VersionedRetrieveInstance)]
        [Route(KnownRoutes.InstanceRoute, Name = KnownRouteNames.RetrieveInstance)]
        [AuditEventType(AuditEventSubType.Retrieve)]
        public async Task<IActionResult> GetInstanceAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid)
        {
            _logger.LogInformation("DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}', instance: '{sopInstanceUid}'.", studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            RetrieveResourceResponse response = await _mediator.RetrieveDicomInstanceAsync(
                studyInstanceUid, seriesInstanceUid, sopInstanceUid, HttpContext.Request.GetAcceptHeaders(), HttpContext.RequestAborted);

            return new ObjectResult(response.ResponseStreams.First())
            {
                StatusCode = (int)HttpStatusCode.OK,
            };
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType((int)HttpStatusCode.NotModified)]
        [HttpGet]
        [VersionedRoute(KnownRoutes.InstanceMetadataRoute)]
        [Route(KnownRoutes.InstanceMetadataRoute)]
        [AuditEventType(AuditEventSubType.RetrieveMetadata)]
        public async Task<IActionResult> GetInstanceMetadataAsync(
            [FromHeader(Name = IfNoneMatch)] string ifNoneMatch,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid)
        {
            _logger.LogInformation("DICOM Web Retrieve Metadata Transaction request received, for study: {studyInstanceUid}, series: {seriesInstanceUid}, instance: {sopInstanceUid}.", studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            RetrieveMetadataResponse response = await _mediator.RetrieveDicomInstanceMetadataAsync(
               studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch, HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [Produces(KnownContentTypes.MultipartRelated)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [VersionedRoute(KnownRoutes.FrameRoute, Name = KnownRouteNames.VersionedRetrieveFrame)]
        [Route(KnownRoutes.FrameRoute, Name = KnownRouteNames.RetrieveFrame)]
        [AuditEventType(AuditEventSubType.Retrieve)]
        public async Task<IActionResult> GetFramesAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            [FromRoute][ModelBinder(typeof(IntArrayModelBinder))] int[] frames)
        {
            _logger.LogInformation("DICOM Web Retrieve Transaction request received, for study: {studyInstanceUid}, series: {seriesInstanceUid}, instance: {sopInstanceUid}, frames: {frames}.", studyInstanceUid, seriesInstanceUid, sopInstanceUid, string.Join(", ", frames ?? Array.Empty<int>()));
            RetrieveResourceResponse response = await _mediator.RetrieveDicomFramesAsync(
                studyInstanceUid, seriesInstanceUid, sopInstanceUid, frames, HttpContext.Request.GetAcceptHeaders(), HttpContext.RequestAborted);

            return CreateResult(response);
        }

        private static IActionResult CreateResult(RetrieveMetadataResponse response)
        {
            return new MetadataResult(response);
        }

        private static IActionResult CreateResult(RetrieveResourceResponse response)
        {
            return new MultipartResult((int)HttpStatusCode.OK, response.ResponseStreams.Select(x => new MultipartItem(response.ContentType, x, response.TransferSyntax)).ToList());
        }
    }
}
