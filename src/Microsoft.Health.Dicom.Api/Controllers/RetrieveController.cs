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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.ModelBinders;
using Microsoft.Health.Dicom.Api.Features.Responses;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ModelStateValidator]
    public class RetrieveController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RetrieveController> _logger;

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
        public async Task<IActionResult> GetStudyAsync([ModelBinder(typeof(TransferSyntaxModelBinder))] string transferSyntax, string studyInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUid}'.");

            RetrieveResourceResponse response = await _mediator.RetrieveDicomStudyAsync(studyInstanceUid, transferSyntax, HttpContext.RequestAborted);

            return CreateResult(response, KnownContentTypes.ApplicationDicom);
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType((int)HttpStatusCode.NotModified)]
        [HttpGet]
        [Route(KnownRoutes.StudyMetadataRoute)]
        public async Task<IActionResult> GetStudyMetadataAsync([ModelBinder(typeof(IfNoneMatchModelBinder))] string ifNoneMatch, string studyInstanceUid)
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
        public async Task<IActionResult> GetSeriesAsync(
            [ModelBinder(typeof(TransferSyntaxModelBinder))] string transferSyntax,
            string studyInstanceUid,
            string seriesInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}'.");

            RetrieveResourceResponse response = await _mediator.RetrieveDicomSeriesAsync(
                studyInstanceUid, seriesInstanceUid, transferSyntax, HttpContext.RequestAborted);

            return CreateResult(response, KnownContentTypes.ApplicationDicom);
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType((int)HttpStatusCode.NotModified)]
        [HttpGet]
        [Route(KnownRoutes.SeriesMetadataRoute)]
        public async Task<IActionResult> GetSeriesMetadataAsync([ModelBinder(typeof(IfNoneMatchModelBinder))] string ifNoneMatch, string studyInstanceUid, string seriesInstanceUid)
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
        public async Task<IActionResult> GetInstanceAsync(
            [ModelBinder(typeof(TransferSyntaxModelBinder))] string transferSyntax,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}', instance: '{sopInstanceUid}'.");

            RetrieveResourceResponse response = await _mediator.RetrieveDicomInstanceAsync(
                studyInstanceUid, seriesInstanceUid, sopInstanceUid, transferSyntax, HttpContext.RequestAborted);

            return CreateResult(response, KnownContentTypes.ApplicationDicom);
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType((int)HttpStatusCode.NotModified)]
        [HttpGet]
        [Route(KnownRoutes.InstanceMetadataRoute)]
        public async Task<IActionResult> GetInstanceMetadataAsync(
            [ModelBinder(typeof(IfNoneMatchModelBinder))] string ifNoneMatch,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Retrieve Metadata Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}', instance: '{sopInstanceUid}'.");

            RetrieveMetadataResponse response = await _mediator.RetrieveDicomInstanceMetadataAsync(
               studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch, HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationOctetStream }, allowSingle: false, allowMultiple: true)]
        [AcceptTransferSyntaxFilter(new[] { DicomTransferSyntaxUids.Original, DicomTransferSyntaxUids.ExplicitVRLittleEndian, }, allowMissing: true)]
        [ProducesResponseType(typeof(Stream), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route(KnownRoutes.FrameRoute)]
        public async Task<IActionResult> GetFramesAsync(
            [ModelBinder(typeof(TransferSyntaxModelBinder))] string transferSyntax,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            [ModelBinder(typeof(IntArrayModelBinder))] int[] frames)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUid}', series: '{seriesInstanceUid}', instance: '{sopInstanceUid}', frames: '{string.Join(", ", frames ?? Array.Empty<int>())}'.");
            RetrieveResourceResponse response = await _mediator.RetrieveDicomFramesAsync(
                studyInstanceUid, seriesInstanceUid, sopInstanceUid, frames, transferSyntax, HttpContext.RequestAborted);

            return CreateResult(response, KnownContentTypes.ApplicationOctetStream);
        }

        private IActionResult CreateResult(RetrieveMetadataResponse response)
        {
            // If cache is valid, just return the 304 status code (Not Modified)
            if (response.IsCacheValid)
            {
                return StatusCode((int)HttpStatusCode.NotModified);
            }
            else
            {
                // If response contains an ETag, add it to the headers.
                if (!string.IsNullOrEmpty(response.ETag))
                {
                    HttpContext.Response.Headers.Add(HeaderNames.ETag, new StringValues(response.ETag));
                }

                return StatusCode((int)HttpStatusCode.OK, response.ResponseMetadata);
            }
        }

        private static IActionResult CreateResult(RetrieveResourceResponse response, string contentType)
        {
            return new MultipartResult((int)HttpStatusCode.OK, response.ResponseStreams.Select(x => new MultipartItem(contentType, x)).ToList());
        }
    }
}
