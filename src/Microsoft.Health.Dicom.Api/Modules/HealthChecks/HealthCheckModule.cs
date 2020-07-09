// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Dicom.Api.Modules.HealthChecks
{
    public class HealthCheckModule : Microsoft.Health.Api.Modules.HealthChecks.HealthCheckModule, IStartupModule
    {
    }
}
