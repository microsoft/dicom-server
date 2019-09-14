// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Messages.Delete;

namespace Microsoft.Health.Dicom.Web.Controllers
{
    public class DicomDeleteController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DicomDeleteController> _logger;

        public DicomDeleteController(IMediator mediator, ILogger<DicomDeleteController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("studies/{studyInstanceUID}")]
        public async Task<IActionResult> DeleteStudyAsync(string studyInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Delete Study request received, with study instance UID '{studyInstanceUID}'.");

            DeleteDicomResourcesResponse deleteResponse = await _mediator.DeleteDicomResourcesAsync(
                studyInstanceUID, cancellationToken: HttpContext.RequestAborted);

            return StatusCode(deleteResponse.StatusCode);
        }

        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("studies/{studyInstanceUID}/series/{seriesUID}")]
        public async Task<IActionResult> DeleteSeriesAsync(string studyInstanceUID, string seriesUID)
        {
            _logger.LogInformation($"DICOM Web Delete Series request received, with study instance UID '{studyInstanceUID}' and series UID '{seriesUID}'.");

            DeleteDicomResourcesResponse deleteResponse = await _mediator.DeleteDicomResourcesAsync(
                studyInstanceUID, seriesUID, cancellationToken: HttpContext.RequestAborted);

            return StatusCode(deleteResponse.StatusCode);
        }

        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("studies/{studyInstanceUID}/series/{seriesUID}/instances/{instanceUID}")]
        public async Task<IActionResult> DeleteInstanceAsync(string studyInstanceUID, string seriesUID, string instanceUID)
        {
            _logger.LogInformation($"DICOM Web Delete Instance request received, with study instance UID '{studyInstanceUID}', series UID '{seriesUID}' and instance UID '{instanceUID}'.");

            DeleteDicomResourcesResponse deleteResponse = await _mediator.DeleteDicomResourcesAsync(
                studyInstanceUID, seriesUID, instanceUID, cancellationToken: HttpContext.RequestAborted);

            return StatusCode(deleteResponse.StatusCode);
        }
    }
}
