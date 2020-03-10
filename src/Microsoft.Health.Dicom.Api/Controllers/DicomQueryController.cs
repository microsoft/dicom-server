// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Api
{
    [Authorize]
    public class DicomQueryController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DicomQueryController> _logger;

        public DicomQueryController(IMediator mediator, ILogger<DicomQueryController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("studies")]
        public async Task<IActionResult> QueryForStudyAsync()
        {
            _logger.LogInformation($"DICOM Web Query Study request received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.AllStudies,
                cancellationToken: HttpContext.RequestAborted);

            return StatusCode(response.StatusCode);
        }

        [HttpGet]
        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("series")]
        public async Task<IActionResult> QueryForSeriesAsync()
        {
            _logger.LogInformation($"DICOM Web Query Series request received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.AllSeries,
                cancellationToken: HttpContext.RequestAborted);

            return StatusCode(response.StatusCode);
        }

        [HttpGet]
        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("studies/{studyInstanceUID}/series")]
        public async Task<IActionResult> QueryForSeriesInStudyAsync(string studyInstanceUID)
        {
            ValidateUID(studyInstanceUID, nameof(studyInstanceUID));

            _logger.LogInformation($"DICOM Web Query Series request for study '{studyInstanceUID}' received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.StudySeries,
                studyInstanceUID: studyInstanceUID,
                cancellationToken: HttpContext.RequestAborted);

            return StatusCode(response.StatusCode);
        }

        [HttpGet]
        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("instances")]
        public async Task<IActionResult> QueryForInstancesAsync()
        {
            _logger.LogInformation($"DICOM Web Query instances request received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.AllInstances,
                cancellationToken: HttpContext.RequestAborted);

            return StatusCode(response.StatusCode);
        }

        [HttpGet]
        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("studies/{studyInstanceUID}/instances")]
        public async Task<IActionResult> QueryForInstancesInStudyAsync(string studyInstanceUID)
        {
            ValidateUID(studyInstanceUID, nameof(studyInstanceUID));

            _logger.LogInformation($"DICOM Web Query Instances for study '{studyInstanceUID}' received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.StudyInstances,
                studyInstanceUID: studyInstanceUID,
                cancellationToken: HttpContext.RequestAborted);

            return StatusCode(response.StatusCode);
        }

        [HttpGet]
        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances")]
        public async Task<IActionResult> QueryForInstancesInSeriesAsync(string studyInstanceUID, string seriesInstanceUID)
        {
            ValidateUID(studyInstanceUID, nameof(studyInstanceUID));
            ValidateUID(seriesInstanceUID, nameof(seriesInstanceUID));

            _logger.LogInformation($"DICOM Web Query Instances for study '{studyInstanceUID}' and series '{seriesInstanceUID}' received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.StudySeriesInstances,
                studyInstanceUID: studyInstanceUID,
                seriesInstanceUID: seriesInstanceUID,
                cancellationToken: HttpContext.RequestAborted);

            return StatusCode(response.StatusCode);
        }

        private static void ValidateUID(string value, string nameOfParam)
        {
            EnsureArg.Matches(value, DicomIdentifierValidator.IdentifierRegex, nameof(nameOfParam));
        }
    }
}
