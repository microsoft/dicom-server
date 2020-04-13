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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.ModelBinders;
using Microsoft.Health.Dicom.Api.Features.Responses;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [Authorize]
    public class DicomRetrieveController : Controller
    {
        private const string TransferSyntaxHeaderName = "transfer-syntax";
        private readonly IMediator _mediator;
        private readonly ILogger<DicomRetrieveController> _logger;

        public DicomRetrieveController(IMediator mediator, ILogger<DicomRetrieveController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationOctetStream, KnownContentTypes.ApplicationDicom)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.PartialContent)]
        [HttpGet]
        [Route(KnownRoutes.StudyRoute, Name = KnownRouteNames.RetrieveStudy)]
        public async Task<IActionResult> GetStudyAsync([FromHeader(Name = TransferSyntaxHeaderName)] string transferSyntax, string studyInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUid}'.");

            DicomRetrieveResourceResponse response = await _mediator.RetrieveDicomStudyAsync(studyInstanceUid, transferSyntax, HttpContext.RequestAborted);
            return CreateResult(response);
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route(KnownRoutes.StudyMetadataRoute)]
        public async Task<IActionResult> GetStudyMetadataAsync(string studyInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Retrieve Metadata Transaction request received, for study: '{studyInstanceUid}'.");

            DicomRetrieveMetadataResponse response = await _mediator.RetrieveDicomStudyMetadataAsync(studyInstanceUid, HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationOctetStream, KnownContentTypes.ApplicationDicom)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.PartialContent)]
        [HttpGet]
        [Route(KnownRoutes.SeriesRoute)]
        public async Task<IActionResult> GetSeriesAsync(
            [FromHeader(Name = TransferSyntaxHeaderName)] string transferSyntax,
            string studyInstanceUid,
            string seriesInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}'.");

            DicomRetrieveResourceResponse response = await _mediator.RetrieveDicomSeriesAsync(
                                studyInstanceUid, seriesInstanceUid, transferSyntax, HttpContext.RequestAborted);
            return CreateResult(response);
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route(KnownRoutes.SeriesMetadataRoute)]
        public async Task<IActionResult> GetSeriesMetadataAsync(string studyInstanceUid, string seriesInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Retrieve Metadata Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}'.");

            DicomRetrieveMetadataResponse response = await _mediator.RetrieveDicomSeriesMetadataAsync(
                studyInstanceUid, seriesInstanceUid, HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationOctetStream, KnownContentTypes.ApplicationDicom)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route(KnownRoutes.InstanceRoute, Name = KnownRouteNames.RetrieveInstance)]
        public async Task<IActionResult> GetInstanceAsync(
            [FromHeader(Name = TransferSyntaxHeaderName)] string transferSyntax,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}', instance: '{sopInstanceUid}'.");

            DicomRetrieveResourceResponse response = await _mediator.RetrieveDicomInstanceAsync(
                            studyInstanceUid, seriesInstanceUid, sopInstanceUid, transferSyntax, HttpContext.RequestAborted);
            return CreateResult(response);
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route(KnownRoutes.InstanceMetadataRoute)]
        public async Task<IActionResult> GetInstanceMetadataAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Retrieve Metadata Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}', instance: '{sopInstanceUid}'.");

            DicomRetrieveMetadataResponse response = await _mediator.RetrieveDicomInstanceMetadataAsync(
               studyInstanceUid, seriesInstanceUid, sopInstanceUid, HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationOctetStream)]
        [ProducesResponseType(typeof(Stream), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route(KnownRoutes.FrameRoute)]
        public async Task<IActionResult> GetFramesAsync(
            [FromHeader(Name = TransferSyntaxHeaderName)] string transferSyntax,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            [ModelBinder(typeof(IntArrayModelBinder))] int[] frames)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}', instance: '{sopInstanceUid}', frames: '{string.Join(", ", frames ?? Array.Empty<int>())}'.");
            DicomRetrieveResourceResponse response = await _mediator.RetrieveDicomFramesAsync(
                            studyInstanceUid, seriesInstanceUid, sopInstanceUid, frames, transferSyntax, HttpContext.RequestAborted);
            return CreateResult(response);
        }

        private IActionResult CreateResult(DicomRetrieveResourceResponse response)
        {
            if (response.ResponseStreams == null)
            {
                return NotFound();
            }

            return new MultipartResult(response.StatusCode, response.ResponseStreams.Select(x => new MultipartItem(KnownContentTypes.ApplicationDicom, x)).ToList());
        }

        private IActionResult CreateResult(DicomRetrieveMetadataResponse resourceResponse)
        {
            if (!resourceResponse.ResponseMetadata.Any())
            {
                return NotFound();
            }

            return StatusCode((int)HttpStatusCode.OK, resourceResponse.ResponseMetadata);
        }
    }
}
