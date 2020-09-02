// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Health.Dicom.Api.Features.Context
{
    public static class DicomRequestContextMiddlewareExtensions
    {
        public static IApplicationBuilder UseDicomRequestContext(
            this IApplicationBuilder builder)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));

            return builder.UseMiddleware<DicomRequestContextMiddleware>();
        }
    }
}
