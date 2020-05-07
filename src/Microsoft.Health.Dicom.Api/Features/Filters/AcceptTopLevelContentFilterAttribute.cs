// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Features.Filters
{
    public class AcceptTopLevelContentFilterAttribute : AcceptContentFilterAttribute
    {
        public AcceptTopLevelContentFilterAttribute(params string[] mediaTypes)
            : base(mediaTypes)
        {
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            IList<MediaTypeHeaderValue> acceptHeaders = context.HttpContext.Request.GetTypedHeaders().Accept;

            bool acceptable = false;

            // Validate the accept headers has one of the specified accepted media types.
            if (acceptHeaders != null && acceptHeaders.Count > 0 && acceptHeaders.Any(x => MediaTypes.Contains(x)))
            {
                acceptable = true;
            }

            if (!acceptable)
            {
                context.Result = new StatusCodeResult(NotAcceptableResponseCode);
            }

            base.OnActionExecuting(context);
        }
    }
}
