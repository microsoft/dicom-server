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
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.ModelBinders;
using Microsoft.Health.Dicom.Api.Features.Responses;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Web.Controllers
{
    public class DicomRetrieveController : Controller
    {
        private const string ApplicationOctetStream = "application/octet-stream";
        private const string ApplicationDicom = "application/dicom";
        private const string ApplicationDicomJson = "application/dicom+json";
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

        [AcceptContentFilter(ApplicationOctetStream, ApplicationDicom)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/")]
        public async Task<IActionResult> GetStudyAsync([FromHeader(Name = TransferSyntaxHeaderName)] string transferSyntax, string studyInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUID}'.");

            RetrieveDicomResourceResponse response = await _mediator.RetrieveDicomStudyAsync(studyInstanceUID, transferSyntax, HttpContext.RequestAborted);
            return ConvertToActionResult(response);
        }

        [AcceptContentFilter(ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/metadata")]
        public async Task<IActionResult> GetStudyMetadataAsync(string studyInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Metadata Transaction request received, for study: '{studyInstanceUID}'.");

            RetrieveDicomMetadataResponse response = await _mediator.RetrieveDicomStudyMetadataAsync(studyInstanceUID, HttpContext.RequestAborted);
            return StatusCode(response.StatusCode, response.ResponseMetadata);
        }

        [AcceptContentFilter(ApplicationOctetStream, ApplicationDicom)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}")]
        public async Task<IActionResult> GetSeriesAsync(
            [FromHeader(Name = TransferSyntaxHeaderName)] string transferSyntax,
            string studyInstanceUID,
            string seriesInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}'.");

            RetrieveDicomResourceResponse response = await _mediator.RetrieveDicomSeriesAsync(
                                studyInstanceUID, seriesInstanceUID, transferSyntax, HttpContext.RequestAborted);
            return ConvertToActionResult(response);
        }

        [AcceptContentFilter(ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/metadata")]
        public async Task<IActionResult> GetSeriesMetadataAsync(string studyInstanceUID, string seriesInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Metadata Transaction request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}'.");

            RetrieveDicomMetadataResponse response = await _mediator.RetrieveDicomSeriesMetadataAsync(
                                studyInstanceUID, seriesInstanceUID, HttpContext.RequestAborted);
            return StatusCode(response.StatusCode, response.ResponseMetadata);
        }

        [AcceptContentFilter(ApplicationOctetStream, ApplicationDicom)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances/{sopInstanceUID}")]
        public async Task<IActionResult> GetInstanceAsync(
            [FromHeader(Name = TransferSyntaxHeaderName)] string transferSyntax,
            string studyInstanceUID,
            string seriesInstanceUID,
            string sopInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}', instance: '{sopInstanceUID}'.");

            RetrieveDicomResourceResponse response = await _mediator.RetrieveDicomInstanceAsync(
                            studyInstanceUID, seriesInstanceUID, sopInstanceUID, transferSyntax, HttpContext.RequestAborted);
            return ConvertToActionResult(response);
        }

        [AcceptContentFilter(ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances/{sopInstanceUID}/metadata")]
        public async Task<IActionResult> GetInstanceMetadataAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Metadata Transaction request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}', instance: '{sopInstanceUID}'.");

            RetrieveDicomMetadataResponse response = await _mediator.RetrieveDicomInstanceMetadataAsync(
                studyInstanceUID, seriesInstanceUID, sopInstanceUID, HttpContext.RequestAborted);
            return StatusCode(response.StatusCode, response.ResponseMetadata);
        }

        [AcceptContentFilter(ApplicationOctetStream)]
        [ProducesResponseType(typeof(Stream), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances/{sopInstanceUID}/frames/{frames}")]
        public async Task<IActionResult> GetFramesAsync(
            [FromHeader(Name = TransferSyntaxHeaderName)] string transferSyntax,
            string studyInstanceUID,
            string seriesInstanceUID,
            string sopInstanceUID,
            [ModelBinder(typeof(IntArrayModelBinder))] int[] frames)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}', instance: '{sopInstanceUID}', frames: '{string.Join(", ", frames ?? Array.Empty<int>())}'.");
            RetrieveDicomResourceResponse response = await _mediator.RetrieveDicomFramesAsync(
                            studyInstanceUID, seriesInstanceUID, sopInstanceUID, frames, transferSyntax, HttpContext.RequestAborted);
            return ConvertToActionResult(response);
        }

        private IActionResult ConvertToActionResult(RetrieveDicomResourceResponse response)
        {
            if (response.ResponseStreams == null)
            {
                return StatusCode(response.StatusCode);
            }

            return new MultipartResult(response.StatusCode, response.ResponseStreams.Select(x => new MultipartItem(ApplicationDicom, x)).ToList());
        }
    }
}
