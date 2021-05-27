// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Serilog.Context;

namespace Microsoft.Health.Dicom.Api.Features.Context
{
    public class DicomRequestContextMiddleware
    {
        private readonly RequestDelegate _next;

        public DicomRequestContextMiddleware(RequestDelegate next)
        {
            EnsureArg.IsNotNull(next, nameof(next));

            _next = next;
        }

        public async Task Invoke(HttpContext context, IDicomRequestContextAccessor dicomRequestContextAccessor)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
            HttpRequest request = context.Request;

            var baseUri = new Uri(UriHelper.BuildAbsolute(
                request.Scheme,
                request.Host,
                request.PathBase));

            var uri = new Uri(UriHelper.BuildAbsolute(
                request.Scheme,
                request.Host,
                request.PathBase,
                request.Path,
                request.QueryString));

            var dicomRequestContext = new DicomRequestContext(
                method: request.Method,
                uri,
                baseUri,
                correlationId: System.Diagnostics.Activity.Current?.RootId,
                context.Request.Headers,
                context.Response.Headers);

            dicomRequestContextAccessor.RequestContext = dicomRequestContext;

            // Adding operation id for this request thread to the log context.
            using (LogContext.PushProperty("operationId", System.Diagnostics.Activity.Current?.RootId))
            {
                // Call the next delegate/middleware in the pipeline
                await _next(context);
            }
        }
    }
}
