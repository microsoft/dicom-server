// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Features.ByteCounter;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Dicom.Api.Features.Context
{
    public class DicomRequestContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IResponseLogStreamFactory _responseLogStreamFactory;

        private static readonly Encoding HeaderEncoding = Encoding.UTF8;
        private static readonly int HeaderDelimiterByteCount = HeaderEncoding.GetByteCount(": ");
        private static readonly int HeaderEndOfLineCharactersByteCount = HeaderEncoding.GetByteCount("\r\n");

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

            ByteCountingStream byteCountingStream = _responseLogStreamFactory.CreateByteCountingResponseLogStream(context.Response.Body);
            context.Response.Body = byteCountingStream;

            try
            {
                // Call the next delegate/middleware in the pipeline
                await _next(context);
            }
            finally
            {
                context.Items["TranscodedSize"] = dicomRequestContextAccessor.RequestContext.BytesTranscoded;
                context.Items["IsTranscodeRequested"] = dicomRequestContextAccessor.RequestContext.IsTranscodeRequested;

                long responseBodySize = byteCountingStream.WrittenByteCount;
                long responseHeaderSize = GetTotalHeaderLength(context.Response.Headers);
                long totalResponseSize = responseBodySize + responseHeaderSize;
                context.Items["ResponseSize"] = totalResponseSize;
            }
        }

        private static long GetTotalHeaderLength(IHeaderDictionary headers)
        {
            // Per https://en.wikipedia.org/wiki/List_of_HTTP_header_fields, each header will be of the form
            // headerKey: headerValues, and terminated by an end-of-line character sequence. The list of headers
            // will be terminated by another end-of-line character sequence.
            EnsureArg.IsNotNull(headers, nameof(headers));

            int headerLength = 0;
            foreach (KeyValuePair<string, StringValues> header in headers)
            {
                headerLength += HeaderEncoding.GetByteCount(header.Key)
                    + HeaderDelimiterByteCount
                    + HeaderEncoding.GetByteCount(header.Value.ToString())
                    + HeaderEndOfLineCharactersByteCount;
            }

            headerLength += HeaderEndOfLineCharactersByteCount;

            return headerLength;
        }
    }
}
