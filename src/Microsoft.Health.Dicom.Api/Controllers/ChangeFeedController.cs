// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ModelStateValidator]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    public class ChangeFeedController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ChangeFeedController> _logger;

        public ChangeFeedController(IMediator mediator, ILogger<ChangeFeedController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(JsonResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [Route(KnownRoutes.ChangeFeed)]
        [AuditEventType(AuditEventSubType.ChangeFeed)]
        public async Task<IActionResult> GetChangeFeed([FromQuery] long offset = 0, [FromQuery] int limit = 10, [FromQuery] bool includeMetadata = true)
        {
            _logger.LogInformation($"Change feed was read with an offset of {offset} and limit of {limit} and metadata is {(includeMetadata ? string.Empty : "not")} included.");

            var response = await _mediator.GetChangeFeed(
                offset,
                limit,
                includeMetadata,
                cancellationToken: HttpContext.RequestAborted);

            return StatusCode((int)HttpStatusCode.OK, response.Entries);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ChangeFeedEntry), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [Route(KnownRoutes.ChangeFeedLatest)]
        [AuditEventType(AuditEventSubType.ChangeFeed)]
        public async Task<IActionResult> GetChangeFeedLatest([FromQuery] bool includeMetadata = true)
        {
            _logger.LogInformation($"Change feed  latest was read and metadata is {(includeMetadata ? string.Empty : "not")} included.");

            var response = await _mediator.GetChangeFeedLatest(
                includeMetadata,
                cancellationToken: HttpContext.RequestAborted);

            return StatusCode((int)HttpStatusCode.OK, response.Entry);
        }
    }
}
