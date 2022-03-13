// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Operations.Management;

namespace Microsoft.Health.Dicom.Operations.Extensions;

internal static class DurableFunctionsContextExtensions
{
    public static bool HasInstanceGuid(this IDurableOrchestrationContext durableOrchestrationContext)
        => Guid.TryParseExact(
            durableOrchestrationContext.InstanceId,
            OperationId.FormatSpecifier,
            out Guid _);

    public static Guid GetInstanceGuid(this IDurableActivityContext durableActivityContext)
        => Guid.ParseExact(durableActivityContext.InstanceId, OperationId.FormatSpecifier);

    // CreatedTime is not preserved between restarts from ContinueAsNew, so we need to save the value if we're going to restart
    public static async Task<DateTime> GetCreatedTimeAsync(this IDurableOrchestrationContext context, RetryOptions retryOptions)
    {
        DurableOrchestrationStatus status = await context.CallActivityWithRetryAsync<DurableOrchestrationStatus>(
            nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
            retryOptions,
            new GetInstanceStatusInput
            {
                InstanceId = context.InstanceId,
                ShowHistory = false,
                ShowHistoryOutput = false,
                ShowInput = false,
            });

        return status.CreatedTime;
    }
}
