// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Features.Filters
{
    public class AcceptContentFilterAttribute : ActionFilterAttribute
    {
        private readonly string _acceptHeaders;

        public AcceptContentFilterAttribute(string acceptHeaders)
        {
            _acceptHeaders = acceptHeaders;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var isValid = context.HttpContext.Request.Headers.TryGetValue(HeaderNames.Accept, out StringValues acceptHeaderValue);

            if (!isValid || !acceptHeaderValue.Contains(_acceptHeaders))
            {
                context.Result = new StatusCodeResult((int)HttpStatusCode.NotAcceptable);
            }

            base.OnActionExecuting(context);
        }
    }
}
