// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Dicom.Functions.Health;

/// <summary>
/// Represents an Azure Function that invokes the registered health checks.
/// </summary>
public class HealthCheckFunction
{
    private readonly HealthCheckMiddleware _middleware;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckFunction"/> class.
    /// </summary>
    /// <param name="options">The health check options.</param>
    /// <param name="service">The <see cref="HealthCheckService"/> that encapsulates the registrations.</param>
    public HealthCheckFunction(IOptions<HealthCheckOptions> options, HealthCheckService service)
        => _middleware = new HealthCheckMiddleware(t => Task.CompletedTask, options, service);

    /// <summary>
    /// Asynchronously checks the health of all registered health checks.
    /// </summary>
    /// <param name="request">The HTTP GET request.</param>
    /// <returns>
    /// A task representing the <see cref="CheckHealthAsync"/> operation.
    /// The value of the <see cref="Task{T}.Result"/> property is an empty result, as the operation
    /// writes directly to the response body.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    [FunctionName(nameof(CheckHealthAsync))]
    public async Task<IActionResult> CheckHealthAsync([HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "health/checks")] HttpRequest request)
    {
        await _middleware.InvokeAsync(EnsureArg.IsNotNull(request, nameof(request)).HttpContext);
        return new EmptyResult(); // Avoid trying to write already written response
    }
}
