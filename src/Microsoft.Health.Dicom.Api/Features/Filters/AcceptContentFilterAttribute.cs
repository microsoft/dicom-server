// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Features.Filters;

public sealed class AcceptContentFilterAttribute : ActionFilterAttribute
{
    private const int NotAcceptableResponseCode = (int)HttpStatusCode.NotAcceptable;

    private readonly HashSet<MediaTypeHeaderValue> _mediaTypes;

    public AcceptContentFilterAttribute(string[] mediaTypes)
    {
        EnsureArg.IsNotNull(mediaTypes, nameof(mediaTypes));
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
        EnsureArg.IsNotNull(context, nameof(context));

        bool acceptable = AcceptHeadersContainKnownTypes(context.HttpContext.Request);

        if (!acceptable)
        {
            context.Result = new StatusCodeResult(NotAcceptableResponseCode);
        }

        base.OnActionExecuting(context);
    }

    private bool AcceptHeadersContainKnownTypes(HttpRequest request)
    {
        IList<MediaTypeHeaderValue> acceptHeaders = ParseAcceptHeaders(request.Headers.Accept);

        foreach (MediaTypeHeaderValue acceptHeader in acceptHeaders)
        {
            if (_mediaTypes.Any(x => acceptHeader.MatchesMediaType(x.MediaType)))
            {
                return true;
            }
        }

        return false;
    }

    private static IList<MediaTypeHeaderValue> ParseAcceptHeaders(StringValues acceptHeaders)
    {
        try
        {
            return MediaTypeHeaderValue.ParseStrictList(acceptHeaders);
        }
        catch (FormatException)
        {
            return new List<MediaTypeHeaderValue>();
        }
    }
}
