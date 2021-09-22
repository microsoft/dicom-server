// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Health.Dicom.Api.Features.ByteCounter;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Dicom.Api.Features.Context
{
    public class DicomRequestContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IResponseLogStreamFactory _responseLogStreamFactory;

        public DicomRequestContextMiddleware(
            RequestDelegate next,
            IResponseLogStreamFactory responseLogStreamFactory)
        {
            EnsureArg.IsNotNull(next, nameof(next));
            EnsureArg.IsNotNull(responseLogStreamFactory, nameof(responseLogStreamFactory));

            _next = next;
            _responseLogStreamFactory = responseLogStreamFactory;
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

            // TODO: replace with code from healthcare-shared-components
            using (ByteCountingStream byteCountingStream = _responseLogStreamFactory.CreateByteCountingResponseLogStream(context.Response.Body))
            {
                context.Response.Body = byteCountingStream;

                try
                {
                    // Call the next delegate/middleware in the pipeline
                    await _next(context);
                }
                finally
                {
                    long responseBodySize = byteCountingStream.WrittenByteCount;
                    long responseHeaderSize = HeaderUtility.GetTotalHeaderLength(context.Response.Headers);
                    long totalResponseSize = responseBodySize + responseHeaderSize;

                    dicomRequestContextAccessor.RequestContext.ResponseSize = totalResponseSize;
                }
            }
        }
    }
}
