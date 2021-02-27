// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Dicom.Api.Features.Context
{
    /// <summary>
    /// Middleware that runs after authentication middleware so that it can retrieved authenticated user claims.
    /// </summary>
    public class DicomRequestContextAfterAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public DicomRequestContextAfterAuthenticationMiddleware(RequestDelegate next)
        {
            EnsureArg.IsNotNull(next, nameof(next));

            _next = next;
        }

        public async Task Invoke(HttpContext context, IDicomRequestContextAccessor dicomRequestContextAccessor)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));

            // Now the authentication is completed successfully, sets the user.
            if (context.User != null)
            {
                dicomRequestContextAccessor.DicomRequestContext.Principal = context.User;
            }

            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }
    }
}
