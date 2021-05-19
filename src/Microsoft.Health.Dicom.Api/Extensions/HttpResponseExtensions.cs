// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    internal static class HttpResponseExtensions
    {
        public static void AddLocationHeader(this HttpResponse response, Uri locationUrl)
        {
            EnsureArg.IsNotNull(response, nameof(response));
            EnsureArg.IsNotNull(locationUrl, nameof(locationUrl));

            response.Headers.Add("Location", Uri.EscapeUriString(locationUrl.ToString()));
        }
    }
}
