// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [Authorize]
    public class DicomStoreController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DicomStoreController> _logger;

        public DicomStoreController(IMediator mediator, ILogger<DicomStoreController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [DisableRequestSizeLimit]
        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.Conflict)]
        [ProducesResponseType((int)HttpStatusCode.UnsupportedMediaType)]
        [HttpPost]
        [Route("studies/{studyInstanceUID?}")]
        public async Task<IActionResult> PostAsync(string studyInstanceUID = null)
        {
            _logger.LogInformation($"DICOM Web Store Transaction request received, with study instance UID '{studyInstanceUID}'.");

            Uri requestBaseUri = GetRequestBaseUri(Request);
            StoreDicomResourcesResponse storeResponse = await _mediator.StoreDicomResourcesAsync(
                                            requestBaseUri, Request.Body, Request.ContentType, studyInstanceUID, HttpContext.RequestAborted);

            return StatusCode(storeResponse.StatusCode, storeResponse.Dataset);
        }

        private static Uri GetRequestBaseUri(HttpRequest request)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsTrue(request.Host.HasValue, nameof(request.Host));

            return new Uri($"{request.Scheme}://{request.Host.Value}/");
        }
    }
}
