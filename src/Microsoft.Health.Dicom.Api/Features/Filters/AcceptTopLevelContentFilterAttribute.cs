// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Features.Filters
{
    public class AcceptTopLevelContentFilterAttribute : ActionFilterAttribute
    {
        private const int NotAcceptableResponseCode = (int)HttpStatusCode.NotAcceptable;

        private readonly HashSet<MediaTypeHeaderValue> _mediaTypes;

        public AcceptTopLevelContentFilterAttribute(params string[] mediaTypes)
        {
            Debug.Assert(mediaTypes.Length > 0, "The accept content type filter must have at least one media type specified.");

            _mediaTypes = new HashSet<MediaTypeHeaderValue>(mediaTypes.Length);

            foreach (var mediaType in mediaTypes)
            {
                if (MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType))
                {
                    _mediaTypes.Add(parsedMediaType);
                }
                else
                {
                    Debug.Assert(false, "The values in the mediaTypes parameter must be parseable by MediaTypeHeaderValue.");
                }
            }
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            IList<MediaTypeHeaderValue> acceptHeaders = context.HttpContext.Request.GetTypedHeaders().Accept;

            bool acceptable = false;

            // Validate the accept headers has one of the specified accepted media types.
            if (acceptHeaders != null && acceptHeaders.Count > 0 && acceptHeaders.Any(x => _mediaTypes.Contains(x)))
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
