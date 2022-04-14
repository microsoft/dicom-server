// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Messages.Export;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Api.Controllers;

[ApiVersion("1.0-prerelease")]
[ApiVersion("1")]
[ServiceFilter(typeof(Features.Audit.AuditLoggingFilterAttribute))]
[ServiceFilter(typeof(PopulateDataPartitionFilterAttribute))]
public class ExportController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ExportController> _logger;
    private readonly bool _enabled;

    public ExportController(
        IMediator mediator,
        IOptions<FeatureConfiguration> featureConfiguration,
        ILogger<ExportController> logger)
    {
        _mediator = EnsureArg.IsNotNull(mediator, nameof(mediator));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _enabled = EnsureArg.IsNotNull(featureConfiguration?.Value.EnableExport, nameof(featureConfiguration)).GetValueOrDefault();
    }

    [HttpPost]
    [BodyModelStateValidator]
    [Produces(KnownContentTypes.ApplicationJson)]
    [Consumes(KnownContentTypes.ApplicationJson)]
    [ProducesResponseType(typeof(OperationReference), (int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [Route(KnownRoutes.ExportInstancesRoute)]
    [VersionedRoute(KnownRoutes.ExportInstancesRoute)]
    [PartitionRoute(KnownRoutes.ExportInstancesRoute)]
    [VersionedPartitionRoute(KnownRoutes.ExportInstancesRoute)]
    [AuditEventType(AuditEventSubType.Export)]
    public async Task<IActionResult> ExportInstancesAsync([Required][FromBody] ExportSpecification spec)
    {
        return await GetResultIfEnabledAsync(
            async (x, token) =>
            {
                EnsureArg.IsNotNull(x, nameof(x));
                _logger.LogInformation("DICOM Web Export request received to export instances from '{Source}' to '{Sink}'.", x.Source.Type, x.Destination.Type);

                ExportInstancesResponse response = await _mediator.ExportInstancesAsync(x, HttpContext.RequestAborted);

                Response.AddLocationHeader(response.Operation.Href);
                return StatusCode((int)HttpStatusCode.Accepted, response.Operation);
            },
            spec);
    }

    private async ValueTask<IActionResult> GetResultIfEnabledAsync<T>(Func<T, CancellationToken, Task<IActionResult>> factoryAsync, T input)
        => _enabled ? await factoryAsync(input, HttpContext.RequestAborted) : NotFound();
}
