// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace Microsoft.Health.Dicom.Core
{
    public class HttpPartitionService
    {
        public HttpPartitionService(IHttpContextAccessor httpContextAccessor)
        {
#pragma warning disable CA1062 // Validate arguments of public methods
            Tenant = httpContextAccessor.HttpContext.Request.Path.ToString();
#pragma warning restore CA1062 // Validate arguments of public methods
        }

        public string Tenant { get; }
    }
}
