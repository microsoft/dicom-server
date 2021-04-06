// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Api.Features.Security
{
    internal class QueryStringValidatorMiddleware
    {
        private const char UnEncodedLessThan = '<';
        private const string EncodedLessThan = "%3c";

        private readonly RequestDelegate _next;

        public QueryStringValidatorMiddleware(RequestDelegate next)
        {
            EnsureArg.IsNotNull(next, nameof(next));

            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            if (context.Request.QueryString.HasValue
                && (context.Request.QueryString.Value.Contains(UnEncodedLessThan, StringComparison.InvariantCulture)
                || context.Request.QueryString.Value.Contains(EncodedLessThan, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new InvalidQueryStringException();
            }

            await _next(context);
        }
    }
}
