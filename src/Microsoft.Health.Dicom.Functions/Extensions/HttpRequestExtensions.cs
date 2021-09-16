// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using EnsureThat;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Health.Dicom.Functions.Extensions
{
    internal static class HttpRequestExtensions
    {
        public static CancellationTokenSource CreateRequestAbortedLinkedTokenSource(this HttpRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            return CancellationTokenSource.CreateLinkedTokenSource(request.HttpContext.RequestAborted, cancellationToken);
        }
    }
}
