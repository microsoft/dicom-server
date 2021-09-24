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

        public static void AddLocationHeader(this HttpResponse response, Uri locationUrl)
        {
            EnsureArg.IsNotNull(response, nameof(response));
            EnsureArg.IsNotNull(locationUrl, nameof(locationUrl));

            response.Headers.Add(HeaderNames.Location, Uri.EscapeUriString(locationUrl.ToString()));
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
    }
}
