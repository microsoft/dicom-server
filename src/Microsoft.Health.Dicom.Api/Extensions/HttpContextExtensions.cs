// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Health.Dicom.Api.Extensions;

internal static class HttpContextExtensions
{
    public static int GetMajorRequestedApiVersion(this HttpContext context)
        => EnsureArg.IsNotNull(context, nameof(context)).GetRequestedApiVersion()?.MajorVersion ?? 1;
}
