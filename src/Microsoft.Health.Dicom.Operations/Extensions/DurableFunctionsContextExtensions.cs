// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Operations.Extensions
{
    internal static class DurableFunctionsContextExtensions
    {
        public static bool HasInstanceGuid(this IDurableOrchestrationContext durableOrchestrationContext)
            => Guid.TryParseExact(
                durableOrchestrationContext.InstanceId,
                OperationId.FormatSpecifier,
                out Guid _);

        public static Guid GetInstanceGuid(this IDurableActivityContext durableActivityContext)
            => Guid.ParseExact(durableActivityContext.InstanceId, OperationId.FormatSpecifier);
    }
}
