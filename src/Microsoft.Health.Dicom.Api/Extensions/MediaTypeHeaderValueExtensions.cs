// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Extensions;

public static class MediaTypeHeaderValueExtensions
{
    public static StringSegment GetParameter(this MediaTypeHeaderValue headerValue, string parameterName, bool tryRemoveQuotes = true)
    {
        EnsureArg.IsNotNull(headerValue, nameof(headerValue));
        EnsureArg.IsNotEmptyOrWhiteSpace(parameterName, nameof(parameterName));
        foreach (NameValueHeaderValue parameter in headerValue.Parameters)
        {
            if (StringSegment.Equals(parameter.Name, parameterName, StringComparison.OrdinalIgnoreCase))
            {
                return tryRemoveQuotes ? HeaderUtilities.RemoveQuotes(parameter.Value) : parameter.Value;
            }
        }

        return StringSegment.Empty;
    }

    public static AcceptHeader ToAcceptHeader(this MediaTypeHeaderValue headerValue)
    {
        EnsureArg.IsNotNull(headerValue, nameof(headerValue));
        StringSegment mediaType = headerValue.MediaType;

        bool isMultipartRelated = StringSegment.Equals(KnownContentTypes.MultipartRelated, mediaType, StringComparison.OrdinalIgnoreCase);
        // handle accept type with no quotes like "multipart/related; type=application/octet-stream; transfer-syntax=*"
        // RFC 2045 is clear that any content type parameter value must be quoted if it contains at least one special character. 
        // However, RFC 2387 which defines `multipart/related` specifies in its ABNF definition of the `type` parameter that quotes are not allowed, although all examples include quotes (Errata 5048). 
        // The DICOMweb standard currently requires quotes, but will soon (CP 1776) allow both forms, so we will allow both.
        bool? startsWithMultiPart = mediaType.Buffer?.StartsWith(KnownContentTypes.MultipartRelated, StringComparison.OrdinalIgnoreCase);
        if (isMultipartRelated)
        {
            mediaType = headerValue.GetParameter(AcceptHeaderParameterNames.Type);
        }
        else if (startsWithMultiPart.HasValue && startsWithMultiPart == true)
        {
            isMultipartRelated = true;
        }

        StringSegment transferSyntax = headerValue.GetParameter(AcceptHeaderParameterNames.TransferSyntax);
        return new AcceptHeader(mediaType, isMultipartRelated ? PayloadTypes.MultipartRelated : PayloadTypes.SinglePart, transferSyntax, headerValue.Quality);
    }
}
