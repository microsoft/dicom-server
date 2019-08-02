// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [Route("studies/{studyInstanceUID}")]
        [Route("studies/{studyInstanceUID}/series/{seriesUID}")]
        [Route("studies/{studyInstanceUID}/series/{seriesUID}/instances/{instanceUID}")]
        public async Task<IActionResult> DeleteStudyAsync(string studyInstanceUID = null, string seriesUID = null, string instanceUID = null)
        {
            _logger.LogInformation($"DICOM Web Delete Study request received, with study instance UID '{studyInstanceUID}', series UID '{seriesUID}' and instance UID '{instanceUID}'.");

            Uri requestBaseUri = GetRequestBaseUri(Request);

            DeleteDicomResourcesResponse deleteResponse = await _mediator.DeleteDicomResourcesAsync(
                requestBaseUri, Request.Body, Request.ContentType, studyInstanceUID, seriesUID, instanceUID, cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

            return StatusCode(deleteResponse.StatusCode);
        }

        private static Uri GetRequestBaseUri(HttpRequest request)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsTrue(request.Host.HasValue, nameof(request.Host));

            return new Uri($"{request.Scheme}://{request.Host.Value}/");
        }
    }
}
