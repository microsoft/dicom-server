// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ModelStateValidator]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    public class CustomTagController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CustomTagController> _logger;

        public CustomTagController(IMediator mediator, ILogger<CustomTagController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        [Route(KnownRoutes.CustomTagRoute)]
        [ProducesResponseType(typeof(JsonResult), (int)HttpStatusCode.Accepted)]
        [AuditEventType(AuditEventSubType.CustomTag)]
        public async Task<IActionResult> PostAsync([FromBody] IEnumerable<CustomTagEntry> customTags)
        {
            _logger.LogInformation("DICOM Web Add Custom Tag request received, with customTags {customTags}.", customTags);

            AddCustomTagResponse response = await _mediator.AddCustomTagsAsync(customTags, HttpContext.RequestAborted);

            return StatusCode(
               (int)HttpStatusCode.Accepted, response);
        }
    }
}
