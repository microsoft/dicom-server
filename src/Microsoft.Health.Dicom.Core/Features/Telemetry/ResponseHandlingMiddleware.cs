// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Telemetry;

public class ResponseHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDicomTelemetryClient _telemetryClient;


    public ResponseHandlingMiddleware(
        RequestDelegate next,
        IDicomTelemetryClient telemetryClient)
    {
        _telemetryClient = EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        _next = EnsureArg.IsNotNull(next, nameof(next));
    }

    public async Task Invoke(HttpContext context)
    {
        // see https://github.com/aspnet/BasicMiddleware/blob/15d5545d59c2f966ed0c730c565c1b9105b258de/src/Microsoft.AspNetCore.Buffering/ResponseBufferingMiddleware.cs
        // and https://github.com/dotnet/aspnetcore/issues/3529
        if (context != null)
        {
            Stream originalBody = context.Response.Body;
            try
            {
                using var newBody = new MemoryStream();

                context.Response.Body = newBody; // give context new body

                //await the response body
                await _next(context);

                var responseSizeBytes = newBody.Length;
                _telemetryClient.TrackTotalResponseBytes(responseSizeBytes);

                newBody.Position = 0; // rewind stream before we write to original steam body
                await newBody.CopyToAsync(originalBody);
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }
    }
}
