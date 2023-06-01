// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace Microsoft.Health.Dicom.Api.Extensions;

internal static class HttpContextExtensions
{
    public static int GetMajorRequestedApiVersion(this HttpContext context)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        var feature = context?.Features.Get<IApiVersioningFeature>();

        if (feature?.RouteParameter != null)
        {
            return feature.RequestedApiVersion?.MajorVersion ?? 1;
        }

        return 1;
    }
}
