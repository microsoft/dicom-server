// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    public static class HttpContextExtensions
    {
        private const string TypeParameter = "type";

        public static string GetAcceptableContentType(this HttpContext context, string[] mediaTypes, bool allowSingle, bool allowMultiple)
        {
            Debug.Assert(mediaTypes.Length > 0, "The accept content type filter must have at least one media type specified.");
            HashSet<MediaTypeHeaderValue> mediaTypeSet = GetMediaTypeSet(mediaTypes);
            IList<MediaTypeHeaderValue> acceptHeaders = context.Request.GetTypedHeaders().Accept;

            // Validate the accept headers has one of the specified accepted media types.
            if (acceptHeaders != null && acceptHeaders.Count > 0)
            {
                foreach (MediaTypeHeaderValue acceptHeader in acceptHeaders)
                {
                    if (allowMultiple && StringSegment.Equals(acceptHeader.MediaType, KnownContentTypes.MultipartRelated, StringComparison.InvariantCultureIgnoreCase))
                    {
                        NameValueHeaderValue typeParameterValue = acceptHeader.Parameters.FirstOrDefault(
                            parameter => StringSegment.Equals(parameter.Name, TypeParameter, StringComparison.InvariantCultureIgnoreCase));

                        if (typeParameterValue != null &&
                            MediaTypeHeaderValue.TryParse(HeaderUtilities.RemoveQuotes(typeParameterValue.Value), out MediaTypeHeaderValue parsedValue) &&
                            mediaTypeSet.Contains(parsedValue))
                        {
                            return parsedValue.ToString();
                        }
                    }

                    if (allowSingle)
                    {
                        string[] split = acceptHeader.ToString().Split(';');
                        foreach (string item in split)
                        {
                            if (MediaTypeHeaderValue.TryParse(item, out MediaTypeHeaderValue parsedMediaType) && mediaTypeSet.Contains(parsedMediaType))
                            {
                                return parsedMediaType.ToString();
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }

        private static HashSet<MediaTypeHeaderValue> GetMediaTypeSet(string[] mediaTypes)
        {
            HashSet<MediaTypeHeaderValue> mediaTypeSet = new HashSet<MediaTypeHeaderValue>(mediaTypes.Length);

            foreach (var mediaType in mediaTypes)
            {
                if (MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType))
                {
                    mediaTypeSet.Add(parsedMediaType);
                }
                else
                {
                    Debug.Assert(false, "The values in the mediaTypes parameter must be parseable by MediaTypeHeaderValue.");
                }
            }

            return mediaTypeSet;
        }
    }
}
