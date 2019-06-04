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
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Formatters;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    public class DicomWebController : Controller
    {
        private const string ApplicationDicomJson = DicomJsonOutputFormatter.ApplicationDicomJson;
        private readonly IMediator _mediator;
        private readonly ILogger<DicomWebController> _logger;

        public DicomWebController(IMediator mediator, ILogger<DicomWebController> logger)
        {
            _mediator = EnsureArg.IsNotNull(mediator, nameof(mediator));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        [DisableRequestSizeLimit]
        [AcceptContentFilter(ApplicationDicomJson)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [HttpPost]
        [Route("studies/{studyInstanceUID?}")]
        public async Task<IActionResult> PostAsync(string studyInstanceUID = null)
        {
            _logger.LogInformation($"DICOM Web STOW-RS request received, with study instance UID '{studyInstanceUID}'.");

            var baseAddress = GetBaseAddress(Request);
            var storeResponse = await _mediator.StoreDicomResourcesAsync(baseAddress, Request.Body, Request.ContentType, studyInstanceUID, HttpContext.RequestAborted);

            return StatusCode(storeResponse.StatusCode, storeResponse.ResponseDataset);
        }

        private static string GetBaseAddress(HttpRequest request)
            => $"{request.Scheme}://{request.Host.Value}";
    }
}
