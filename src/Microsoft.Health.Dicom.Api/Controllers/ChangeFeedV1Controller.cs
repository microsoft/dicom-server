// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Conventions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Messages.ChangeFeed;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.ChangeFeed;
using Microsoft.Health.Dicom.Core.Web;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers;

[ApiVersionRange(end: 1)]
[QueryModelStateValidator]
[ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
public class ChangeFeedV1Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChangeFeedV1Controller> _logger;

    public ChangeFeedV1Controller(IMediator mediator, ILogger<ChangeFeedV1Controller> logger)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [Produces(KnownContentTypes.ApplicationJson)]
    [ProducesResponseType(typeof(IEnumerable<ChangeFeedEntry>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [VersionedRoute(KnownRoutes.ChangeFeed)]
    [AuditEventType(AuditEventSubType.ChangeFeed)]
    public async Task<IActionResult> GetChangeFeedAsync(
        [FromQuery][Range(0, int.MaxValue)] long offset = 0,
        [FromQuery][Range(1, 100)] int limit = 10,
        [FromQuery] bool includeMetadata = true)
    {
        _logger.LogInformation(
            "Change feed was read with an offset of {Offset} and limit of {Limit}. Metadata is {MetadataStatus}.",
            offset,
            limit,
            includeMetadata ? "included" : "not included");

        ChangeFeedResponse response = await _mediator.GetChangeFeed(
            DateTimeOffsetRange.MaxValue,
            offset,
            limit,
            includeMetadata,
            ChangeFeedOrder.Sequence,
            cancellationToken: HttpContext.RequestAborted);

        return StatusCode((int)HttpStatusCode.OK, response.Entries);
    }

    [HttpGet]
    [Produces(KnownContentTypes.ApplicationJson)]
    [ProducesResponseType(typeof(ChangeFeedEntry), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [VersionedRoute(KnownRoutes.ChangeFeedLatest)]
    [AuditEventType(AuditEventSubType.ChangeFeed)]
    public async Task<IActionResult> GetChangeFeedLatestAsync([FromQuery] bool includeMetadata = true)
    {
        _logger.LogInformation("Change feed latest was read and metadata is {Metadata} included.", includeMetadata ? string.Empty : "not");

        ChangeFeedLatestResponse response = await _mediator.GetChangeFeedLatest(
            includeMetadata,
            ChangeFeedOrder.Sequence,
            cancellationToken: HttpContext.RequestAborted);

        return StatusCode((int)HttpStatusCode.OK, response.Entry);
    }
}
