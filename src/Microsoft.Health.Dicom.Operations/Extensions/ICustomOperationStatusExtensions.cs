// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Operations.Management;

namespace Microsoft.Health.Dicom.Operations.Extensions
{
    internal static class ICustomOperationStatusExtensions
    {
        // CreatedTime is not preserved between restarts from ContinueAsNew, so we need to save the value if we're going to restart
        public static async Task UpdateCreatedTimeAsync(this ICustomOperationStatus customStatus, IDurableOrchestrationContext context, RetryOptions retryOptions)
        {
            if (!customStatus.CreatedTime.HasValue)
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

                customStatus.CreatedTime = status.CreatedTime;
            }
        }
    }
}
