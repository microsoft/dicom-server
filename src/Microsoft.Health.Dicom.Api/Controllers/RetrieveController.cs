// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ModelStateValidator]
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

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicom }, allowSingle: false, allowMultiple: true)]
        [AcceptTransferSyntaxFilter(new[] { DicomTransferSyntaxUids.Original })]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route(KnownRoutes.StudyRoute, Name = KnownRouteNames.RetrieveStudy)]
        [AuditEventType(AuditEventSubType.Retrieve)]
#pragma warning disable CA1801 // Review unused parameters
        public async Task<IActionResult> GetStudyAsync([ModelBinder(typeof(TransferSyntaxModelBinder))] string transferSyntax, string studyInstanceUid)
#pragma warning restore CA1801 // Review unused parameters
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUid}'.");

            RetrieveResourceResponse response = await _mediator.RetrieveDicomStudyAsync(studyInstanceUid, HttpContext.Request.GetAcceptHeaders(), HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType((int)HttpStatusCode.NotModified)]
        [HttpGet]
        [Route(KnownRoutes.StudyMetadataRoute)]
        [AuditEventType(AuditEventSubType.RetrieveMetadata)]
        public async Task<IActionResult> GetStudyMetadataAsync([FromHeader(Name = IfNoneMatch)] string ifNoneMatch, string studyInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Retrieve Metadata Transaction request received, for study: '{studyInstanceUid}'.");

            RetrieveMetadataResponse response = await _mediator.RetrieveDicomStudyMetadataAsync(studyInstanceUid, ifNoneMatch, HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicom }, allowSingle: false, allowMultiple: true)]
        [AcceptTransferSyntaxFilter(new[] { DicomTransferSyntaxUids.Original })]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route(KnownRoutes.SeriesRoute)]
        [AuditEventType(AuditEventSubType.Retrieve)]
#pragma warning disable CA1801 // Remove unused parameter
        public async Task<IActionResult> GetSeriesAsync(
            [ModelBinder(typeof(TransferSyntaxModelBinder))] string transferSyntax,
            string studyInstanceUid,
            string seriesInstanceUid)
        {
#pragma warning restore CA1801 // Remove unused parameter
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}'.");

            RetrieveResourceResponse response = await _mediator.RetrieveDicomSeriesAsync(
                studyInstanceUid, seriesInstanceUid, HttpContext.Request.GetAcceptHeaders(), HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType((int)HttpStatusCode.NotModified)]
        [HttpGet]
        [Route(KnownRoutes.SeriesMetadataRoute)]
        [AuditEventType(AuditEventSubType.RetrieveMetadata)]
        public async Task<IActionResult> GetSeriesMetadataAsync([FromHeader(Name = IfNoneMatch)] string ifNoneMatch, string studyInstanceUid, string seriesInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Retrieve Metadata Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}'.");

            RetrieveMetadataResponse response = await _mediator.RetrieveDicomSeriesMetadataAsync(
                studyInstanceUid, seriesInstanceUid, ifNoneMatch, HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicom }, allowSingle: true, allowMultiple: false)]
        [AcceptTransferSyntaxFilter(new[] { DicomTransferSyntaxUids.Original })]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route(KnownRoutes.InstanceRoute, Name = KnownRouteNames.RetrieveInstance)]
        [AuditEventType(AuditEventSubType.Retrieve)]
#pragma warning disable CA1801 // Remove unused parameter
        public async Task<IActionResult> GetInstanceAsync(
            [ModelBinder(typeof(TransferSyntaxModelBinder))] string transferSyntax,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid)
        {
#pragma warning restore CA1801 // Remove unused parameter
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}', instance: '{sopInstanceUid}'.");

            RetrieveResourceResponse response = await _mediator.RetrieveDicomInstanceAsync(
                studyInstanceUid, seriesInstanceUid, sopInstanceUid, HttpContext.Request.GetAcceptHeaders(), HttpContext.RequestAborted);

            return new ObjectResult(response.ResponseStreams.First())
            {
                StatusCode = (int)HttpStatusCode.OK,
            };
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType((int)HttpStatusCode.NotModified)]
        [HttpGet]
        [Route(KnownRoutes.InstanceMetadataRoute)]
        [AuditEventType(AuditEventSubType.RetrieveMetadata)]
        public async Task<IActionResult> GetInstanceMetadataAsync(
            [FromHeader(Name = IfNoneMatch)] string ifNoneMatch,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Retrieve Metadata Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}', instance: '{sopInstanceUid}'.");

            RetrieveMetadataResponse response = await _mediator.RetrieveDicomInstanceMetadataAsync(
               studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch, HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [ProducesResponseType(typeof(Stream), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route(KnownRoutes.FrameRoute)]
        [AuditEventType(AuditEventSubType.Retrieve)]
        public async Task<IActionResult> GetFramesAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            [ModelBinder(typeof(IntArrayModelBinder))] int[] frames)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}', instance: '{sopInstanceUid}', frames: '{string.Join(", ", frames ?? Array.Empty<int>())}'.");
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
            return new MultipartResult((int)HttpStatusCode.OK, response.ResponseStreams.Select(x => new MultipartItem(response.ContentType, x)).ToList());
        }
    }
}
