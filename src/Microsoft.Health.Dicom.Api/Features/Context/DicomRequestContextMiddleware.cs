// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Task = System.Threading.Tasks.Task;

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

            string baseUriInString = UriHelper.BuildAbsolute(
                request.Scheme,
                request.Host,
                request.PathBase);

            string uriInString = UriHelper.BuildAbsolute(
                request.Scheme,
                request.Host,
                request.PathBase,
                request.Path,
                request.QueryString);

            var dicomRequestContext = new DicomRequestContext(
                method: request.Method,
                uriString: uriInString,
                baseUriString: baseUriInString,
                requestHeaders: context.Request.Headers,
                responseHeaders: context.Response.Headers);

            dicomRequestContextAccessor.DicomRequestContext = dicomRequestContext;

            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }
    }
}
