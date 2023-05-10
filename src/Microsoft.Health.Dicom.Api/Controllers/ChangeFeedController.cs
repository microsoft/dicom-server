// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Api.Models;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Messages.ChangeFeed;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Web;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers;

[QueryModelStateValidator]
[ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
public class ChangeFeedController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChangeFeedController> _logger;

    public ChangeFeedController(IMediator mediator, ILogger<ChangeFeedController> logger)
    {
        _mediator = EnsureArg.IsNotNull(mediator, nameof(mediator));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    [HttpGet]
    [MapToApiVersion("1.0-prerelease")]
    [MapToApiVersion("1.0")]
    [Produces(KnownContentTypes.ApplicationJson)]
    [ProducesResponseType(typeof(IEnumerable<ChangeFeedEntry>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [VersionedRoute(KnownRoutes.ChangeFeed)]
    [AuditEventType(AuditEventSubType.ChangeFeed)]
    public Task<IActionResult> GetChangeFeedAsync(
        [FromQuery][Range(0, long.MaxValue)] long offset = 0,
        [FromQuery][Range(1, 100)] int limit = 10,
        [FromQuery] bool includeMetadata = true)
    {
        return GetChangeFeedAsync(TimeRange.MaxValue, offset, limit, includeMetadata, HttpContext.RequestAborted);
    }

    [HttpGet]
    [MapToApiVersion("2.0")]
    [Produces(KnownContentTypes.ApplicationJson)]
    [ProducesResponseType(typeof(IEnumerable<ChangeFeedEntry>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [VersionedRoute(KnownRoutes.ChangeFeed)]
    [AuditEventType(AuditEventSubType.ChangeFeed)]
    public Task<IActionResult> GetChangeFeedAsync(
        [FromQuery] WindowedPaginationOptions options,
        [FromQuery] bool includeMetadata = true)
    {
        EnsureArg.IsNotNull(options, nameof(options));
        return GetChangeFeedAsync(options.Window, options.Offset, options.Limit, includeMetadata, HttpContext.RequestAborted);
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
            cancellationToken: HttpContext.RequestAborted);

        return StatusCode((int)HttpStatusCode.OK, response.Entry);
    }

    private async Task<IActionResult> GetChangeFeedAsync(
        TimeRange range,
        long offset,
        int limit,
        bool includeMetadata,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Change feed was read for {Window} with an offset of {Offset} and limit of {Limit}. Metadata is {MetadataStatus}.",
            range,
            offset,
            limit,
            includeMetadata ? "included" : "not included");

        ChangeFeedResponse response = await _mediator.GetChangeFeed(
            range,
            offset,
            limit,
            includeMetadata,
            cancellationToken);

        return StatusCode((int)HttpStatusCode.OK, response.Entries);
    }
}
