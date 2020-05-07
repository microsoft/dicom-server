// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Features.Filters
{
    public class AcceptContentFilterAttribute : ActionFilterAttribute
    {
        protected const int NotAcceptableResponseCode = (int)HttpStatusCode.NotAcceptable;
        protected const string TypeParameter = "type";

        public AcceptContentFilterAttribute(params string[] mediaTypes)
        {
            Debug.Assert(mediaTypes.Length > 0, "The accept content type filter must have at least one media type specified.");

            MediaTypes = new HashSet<MediaTypeHeaderValue>(mediaTypes.Length);

            foreach (var mediaType in mediaTypes)
            {
                if (MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType))
                {
                    MediaTypes.Add(parsedMediaType);
                }
                else
                {
                    Debug.Assert(false, "The values in the mediaTypes parameter must be parseable by MediaTypeHeaderValue.");
                }
            }
        }

        protected HashSet<MediaTypeHeaderValue> MediaTypes
        {
            get;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            IList<MediaTypeHeaderValue> acceptHeaders = context.HttpContext.Request.GetTypedHeaders().Accept;

            bool acceptable = false;

            // Validate the accept headers has one of the specified accepted media types.
            if (acceptHeaders != null && acceptHeaders.Count > 0)
            {
                var multipartHeaders = acceptHeaders.Where(x => StringSegment.Equals(x.MediaType, KnownContentTypes.MultipartRelated, StringComparison.InvariantCultureIgnoreCase)).ToList();

                if (multipartHeaders.Count > 0)
                {
                    IEnumerable<MediaTypeHeaderValue> prospectiveTypes = multipartHeaders.SelectMany(
                        x => x.Parameters.Where(p => StringSegment.Equals(p.Name, TypeParameter, StringComparison.InvariantCultureIgnoreCase))
                            .Select(p => MediaTypeHeaderValue.TryParse(p.Value.ToString().Trim('"'), out MediaTypeHeaderValue parsedValue) ? parsedValue : null));

                    if (prospectiveTypes.Any(x => MediaTypes.Contains(x)))
                    {
                        acceptable = true;
                    }
                }

                if (!acceptable && acceptHeaders.Any(x => MediaTypes.Contains(x)))
                {
                    acceptable = true;
                }
            }

            if (!acceptable)
            {
                context.Result = new StatusCodeResult(NotAcceptableResponseCode);
            }

            base.OnActionExecuting(context);
        }
    }
}
