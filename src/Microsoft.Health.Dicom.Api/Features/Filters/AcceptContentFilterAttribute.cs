// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Features.Filters
{
    public class AcceptContentFilterAttribute : ActionFilterAttribute
    {
        private const int NotAcceptableResponseCode = (int)HttpStatusCode.NotAcceptable;
        private readonly IReadOnlyCollection<MediaTypeHeaderValue> _mediaTypes;

        public AcceptContentFilterAttribute(params string[] mediaTypes)
        {
            EnsureArg.IsTrue(mediaTypes.Length > 0, nameof(mediaTypes));

            var parsed = new List<MediaTypeHeaderValue>(mediaTypes.Length);

            foreach (var mediaType in mediaTypes)
            {
                EnsureArg.IsTrue(MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType), nameof(mediaTypes));
                parsed.Add(parsedMediaType);
            }

            _mediaTypes = parsed;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            IList<MediaTypeHeaderValue> acceptHeaders = context.HttpContext.Request.GetTypedHeaders().Accept;

            // Validate the accept headers has one of the specified accepted media types.
            if (acceptHeaders == null || !acceptHeaders.Any(x => _mediaTypes.Any(y => x.IsSubsetOf(y))))
            {
                context.Result = new StatusCodeResult(NotAcceptableResponseCode);
            }

            base.OnActionExecuting(context);
        }
    }
}
