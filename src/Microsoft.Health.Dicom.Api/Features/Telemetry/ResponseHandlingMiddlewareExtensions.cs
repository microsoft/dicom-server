// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Health.Dicom.Api.Features.Telemetry;

public static class ResponseTelemetryHandlingMiddlewareExtension
{
    public static IApplicationBuilder UseResponseTelemetryHandling(this IApplicationBuilder builder)
    {
        EnsureArg.IsNotNull(builder);
        return builder.UseMiddleware<ResponseHandlingMiddleware>();
    }
}
