// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Dicom.Api.Modules.HealthChecks
{
    internal sealed class CachedHealthCheck : Microsoft.Health.Api.Features.HealthChecks.CachedHealthCheck
    {
        public CachedHealthCheck(IServiceProvider provider, Func<IServiceProvider, IHealthCheck> healthCheck, ILogger<CachedHealthCheck> logger)
            : base(provider, healthCheck, logger)
        {
        }
    }
}
