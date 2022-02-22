// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    internal static class HttpResponseExtensions
    {
        public const string ErroneousAttributesHeader = "erroneous-dicom-attributes";

        private static readonly Uri ExampleRoot = new Uri("https://example.com/", UriKind.Absolute);

        public static void AddLocationHeader(this HttpResponse response, Uri locationUrl)
        {
            EnsureArg.IsNotNull(response, nameof(response));
            EnsureArg.IsNotNull(locationUrl, nameof(locationUrl));

            response.Headers.Add(HeaderNames.Location, locationUrl.IsAbsoluteUri ? locationUrl.AbsoluteUri : GetRelativeUri(locationUrl));
        }

        public static bool TryAddErroneousAttributesHeader(this HttpResponse response, IReadOnlyCollection<string> erroneousAttributes)
        {
            EnsureArg.IsNotNull(response, nameof(response));
            EnsureArg.IsNotNull(erroneousAttributes, nameof(erroneousAttributes));
            if (erroneousAttributes.Count == 0)
            {
                return false;
            }

            response.Headers.Add(ErroneousAttributesHeader, string.Join(",", erroneousAttributes));
            return true;
        }

        private static string GetRelativeUri(Uri uri)
        // ExampleRoot is necessary as GetComponents, like many of the URI members, throws an exception
        // when used on relative URI instances. As a workaround, we make it absolute temporarily.
        { return new Uri(ExampleRoot, uri).GetComponents(UriComponents.AbsoluteUri & ~UriComponents.SchemeAndServer & ~UriComponents.UserInfo, UriFormat.UriEscaped); }
    }
}
