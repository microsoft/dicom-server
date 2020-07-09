// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Dicom.Api.Modules.HealthChecks
{
    public class HealthCheckConfiguration : Microsoft.Health.Api.Modules.HealthChecks.HealthCheckConfiguration, IPostConfigureOptions<HealthCheckServiceOptions>
    {
        public HealthCheckConfiguration(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
    }
}
