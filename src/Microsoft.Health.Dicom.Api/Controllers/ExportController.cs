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
using Microsoft.Health.Dicom.Api.Features.Conventions;
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

/// <summary>
/// Represents an API controller for export operations.
/// </summary>
[IntroducedInApiVersion(1)]
[ServiceFilter(typeof(Features.Audit.AuditLoggingFilterAttribute))]
[ServiceFilter(typeof(PopulateDataPartitionFilterAttribute))]
public class ExportController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ExportController> _logger;
    private readonly bool _enabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportController"/> class based on the given options.
    /// </summary>
    /// <param name="mediator">An <see cref="IMediator"/> used to send requests.</param>
    /// <param name="options">Options concerning which features are enabled.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="mediator"/>, <paramref name="options"/>, or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    public ExportController(
        IMediator mediator,
        IOptions<FeatureConfiguration> options,
        ILogger<ExportController> logger)
    {
        _mediator = EnsureArg.IsNotNull(mediator, nameof(mediator));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _enabled = EnsureArg.IsNotNull(options?.Value?.EnableExport, nameof(options)).GetValueOrDefault();
    }

    /// <summary>
    /// Asynchronously starts the export operation.
    /// </summary>
    /// <param name="specification">The specification that details the source and destination for the export.</param>
    /// <returns>
    /// A task that represents the asynchronous export operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains the <see cref="IActionResult"/>. Upon success, the result will contain an
    /// <see cref="OperationReference"/> detailing the new export operation instance. Otherwise, the status code
    /// provides details as to why the request failed.
    /// </returns>
    [HttpPost]
    [BodyModelStateValidator]
    [Produces(KnownContentTypes.ApplicationJson)]
    [Consumes(KnownContentTypes.ApplicationJson)]
    [ProducesResponseType(typeof(OperationReference), (int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [VersionedRoute(KnownRoutes.ExportInstancesRoute)]
    [VersionedPartitionRoute(KnownRoutes.ExportInstancesRoute)]
    [AuditEventType(AuditEventSubType.Export)]
    public async Task<IActionResult> ExportAsync([Required][FromBody] ExportSpecification specification)
    {
        EnsureArg.IsNotNull(specification, nameof(specification));

        return await GetResultIfEnabledAsync(
            async (x, token) =>
            {
                _logger.LogInformation("DICOM Web Export request received to export instances from '{Source}' to '{Sink}'.", x.Source.Type, x.Destination.Type);

                ExportResponse response = await _mediator.ExportAsync(x, token);

                Response.AddLocationHeader(response.Operation.Href);
                return StatusCode((int)HttpStatusCode.Accepted, response.Operation);
            },
            specification);
    }

    private async ValueTask<IActionResult> GetResultIfEnabledAsync<T>(Func<T, CancellationToken, Task<IActionResult>> factoryAsync, T input)
        => _enabled ? await factoryAsync(input, HttpContext.RequestAborted) : NotFound();
}
