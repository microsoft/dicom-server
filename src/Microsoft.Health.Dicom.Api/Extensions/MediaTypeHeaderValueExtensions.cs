// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    public static class MediaTypeHeaderValueExtensions
    {
        public static StringSegment GetParameter(this MediaTypeHeaderValue headerValue, string parameterName, bool tryRemoveQuotes = true)
        {
            Debug.Assert(parameterName != null, $"{nameof(parameterName)} should not be null");
            foreach (NameValueHeaderValue parameter in headerValue.Parameters)
            {
                if (StringSegment.Equals(parameter.Name, parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return tryRemoveQuotes ? HeaderUtilities.RemoveQuotes(parameter.Value) : parameter.Value;
                }
            }

            return null;
        }

        public static AcceptHeader ToAcceptHeader(this MediaTypeHeaderValue headerValue)
        {
            StringSegment mediaType = headerValue.MediaType;
            bool isMultipart = StringSegment.Equals(KnownContentTypes.MultipartRelated, mediaType, StringComparison.OrdinalIgnoreCase);
            if (isMultipart)
            {
                mediaType = headerValue.GetParameter(AcceptHeaderParameters.Type);
            }

            StringSegment transferSytnax = headerValue.GetParameter(AcceptHeaderParameters.TransferSyntax);
            return new AcceptHeader(mediaType, isMultipart, transferSytnax, headerValue.Quality);
        }
    }
}
