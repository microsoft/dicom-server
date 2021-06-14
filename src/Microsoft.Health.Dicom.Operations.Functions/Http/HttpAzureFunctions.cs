// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using EnsureThat;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Health.Dicom.Operations.Functions.Http
{
    internal static class HttpAzureFunctions
    {
        public static CancellationTokenSource CreateCancellationSource(HttpRequest request, CancellationToken hostCancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            return CancellationTokenSource.CreateLinkedTokenSource(request.HttpContext.RequestAborted, hostCancellationToken);
        }
    }
}
