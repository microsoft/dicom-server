// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Http;
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

        [ProducesResponseType(typeof(JsonResult), (int)HttpStatusCode.Accepted)]
        [HttpPost]
        [Route(KnownRoutes.CustomTagRoute)]
        [AuditEventType(AuditEventSubType.AddCustomTag)]
        public async Task<IActionResult> PostAsync([FromBody] IEnumerable<CustomTagEntry> customTags)
        {
            _logger.LogInformation("DICOM Web Add Custom Tag request received, with customTags {customTags}.", customTags);

            AddCustomTagResponse response = await _mediator.AddCustomTagsAsync(customTags, HttpContext.RequestAborted);

            return StatusCode(
               (int)HttpStatusCode.Accepted, response);
        }

        /// <summary>
        /// Handles requests to get all custom tags.
        /// </summary>
        /// <returns>
        /// Returns Bad Request if given path can't be parsed. Returns Not Found if given path doesn't map to a stored
        /// custom tag or if no custom tags are stored. Returns OK with a JSON body of all tags in other cases.
        /// </returns>
        [ProducesResponseType(typeof(JsonResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet]
        [Route(KnownRoutes.CustomTagRoute)]
        [AuditEventType(AuditEventSubType.GetAllCustomTags)]
        public async Task<IActionResult> GetAllTagsAsync()
        {
            _logger.LogInformation("DICOM Web Get Custom Tag request received for all custom tags");

            GetAllCustomTagsResponse response = await _mediator.GetAllCustomTagsAsync(HttpContext.RequestAborted);

            return StatusCode(
                (int)HttpStatusCode.OK, response);
        }

        /// <summary>
        /// Handles requests to get individual custom tags.
        /// </summary>
        /// <param name="tagPath">Path for requested custom tag.</param>
        /// <returns>
        /// Returns Bad Request if given path can't be parsed. Returns Not Found if given path doesn't map to a stored
        /// custom tag. Returns OK with a JSON body of requested tag in other cases.
        /// </returns>
        [ProducesResponseType(typeof(JsonResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet]
        [Route(KnownRoutes.GetCustomTagRoute)]
        [AuditEventType(AuditEventSubType.GetCustomTag)]
        public async Task<IActionResult> GetTagAsync(string tagPath)
        {
            _logger.LogInformation("DICOM Web Get Custom Tag request received for custom tag: {tagPath}");

            GetCustomTagResponse response = await _mediator.GetCustomTagAsync(tagPath, HttpContext.RequestAborted);

            return StatusCode(
                (int)HttpStatusCode.OK, response);
        }
    }
}
