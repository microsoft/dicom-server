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
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Api.Models.Binding;
using Microsoft.Health.Dicom.Core.Exceptions;
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
        [FromQuery][Range(1, int.MaxValue)] int? limit = null,
        [FromQuery][ModelBinder(typeof(Iso8601Binder))] DateTimeOffset? startTime = null,
        [FromQuery][ModelBinder(typeof(Iso8601Binder))] DateTimeOffset? endTime = null,
        [FromQuery] bool includeMetadata = true)
    {
        int updatedLimit = GetLimit(limit, HttpContext.GetRequestedApiVersion()?.MajorVersion ?? 1);
        var range = new DateTimeOffsetRange(startTime ?? DateTimeOffset.MinValue, endTime ?? DateTimeOffset.MaxValue);

        _logger.LogInformation(
            "Change feed was read for {Window} with an offset of {Offset} and limit of {Limit}. Metadata is {MetadataStatus}.",
            range,
            offset,
            updatedLimit,
            includeMetadata ? "included" : "not included");

        ChangeFeedResponse response = await _mediator.GetChangeFeed(
            range,
            offset,
            updatedLimit,
            includeMetadata,
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
            cancellationToken: HttpContext.RequestAborted);

        return StatusCode((int)HttpStatusCode.OK, response.Entry);
    }

    private static int GetLimit(int? userValue, int majorVersion)
    {
        // Default limit for v2 and beyond aligns with QIDO while previous versions used 10
        const int OldDefault = 10;
        const int NewDefault = 100;
        const int OldMax = 100;
        const int NewMax = 200;

        (int defaultValue, int maxValue) = majorVersion switch
        {
            1 => (OldDefault, OldMax),
            _ => (NewDefault, NewMax),
        };

        if (userValue > maxValue)
            throw new ChangeFeedLimitOutOfRangeException(maxValue);

        return userValue ?? defaultValue;
    }
}
