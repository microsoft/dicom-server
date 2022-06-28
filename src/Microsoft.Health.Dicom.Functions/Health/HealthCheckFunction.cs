// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Dicom.Functions.Health;

public class HealthCheckFunction
{
    private readonly HealthCheckMiddleware _middleware;

    public HealthCheckFunction(IOptions<HealthCheckOptions> options, HealthCheckService service)
        => _middleware = new HealthCheckMiddleware(t => Task.CompletedTask, options, service);

    [FunctionName(nameof(CheckHealthAsync))]
    public Task CheckHealthAsync([HttpTrigger(AuthorizationLevel.Anonymous, Route = "health/checks")] HttpRequest request)
        => _middleware.InvokeAsync(EnsureArg.IsNotNull(request, nameof(request)).HttpContext);
}
