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

namespace Microsoft.Health.Dicom.Api.Extensions
{
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
            if (isMultipartRelated)
            {
                mediaType = headerValue.GetParameter(AcceptHeaderParameterNames.Type);
            }

            StringSegment transferSyntax = headerValue.GetParameter(AcceptHeaderParameterNames.TransferSyntax);
            return new AcceptHeader(mediaType, isMultipartRelated ? PayloadTypes.MultipartRelated : PayloadTypes.SinglePart, transferSyntax, headerValue.Quality);
        }
    }
}
