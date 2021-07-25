// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Extensions
{
    internal static class DurableFunctionsContextExtensions
    {
        public static bool HasInstanceGuid(this IDurableOrchestrationContext durableOrchestrationContext)
            => Guid.TryParse(durableOrchestrationContext.InstanceId, out Guid _);

        public static Guid GetInstanceGuid(this IDurableActivityContext durableActivityContext)
            => Guid.Parse(durableActivityContext.InstanceId);
    }
}
