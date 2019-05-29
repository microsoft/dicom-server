// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Dicom.Web.Controllers
{
    public class DicomWebController : Controller
    {
        private readonly ILogger<DicomWebController> _logger;

        public DicomWebController(ILogger<DicomWebController> logger)
        {
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        [DisableRequestSizeLimit]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [HttpPost]
        [Route("studies/{studyInstanceUID?}")]
        public IActionResult Post(string studyInstanceUID = null)
        {
            _logger.LogInformation($"DICOM Web STOW-RS request received, with study instance UID '{studyInstanceUID}'.");
            return Ok();
        }
    }
}
