// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Api.Features.Security;

namespace Microsoft.AspNetCore.Builder
{
    internal static class QueryStringValidatorMiddlewareExtension
    {
        public static IApplicationBuilder UseQueryStringValidator(this IApplicationBuilder builder)
        {
            EnsureArg.IsNotNull(builder);
            return builder.UseMiddleware<QueryStringValidatorMiddleware>();
        }
    }
}
