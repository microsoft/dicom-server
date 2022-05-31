// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Functions.Client.Extensions;

internal static class OperationQueryConditionExtensions
{
    public static OrchestrationStatusQueryCondition ForDurableFunctions<T>(this OperationQueryCondition<T> query, string continuationToken = null)
    {
        // TODO: Modify page size?
        EnsureArg.IsNotNull(query, nameof(query));
        return new OrchestrationStatusQueryCondition
        {
            ContinuationToken = continuationToken,
            CreatedTimeFrom = query.CreatedTimeFrom,
            CreatedTimeTo = query.CreatedTimeTo,
            RuntimeStatus = query.Statuses.Select(ToOrchestrationRuntimeStatus).ToList(), // Aggressively resolve
            ShowInput = true,
        };
    }

    // TODO: Move to healthcare-shared-components
    private static OrchestrationRuntimeStatus ToOrchestrationRuntimeStatus(OperationStatus status)
        => status switch
        {
            OperationStatus.Canceled => OrchestrationRuntimeStatus.Canceled,
            OperationStatus.Completed => OrchestrationRuntimeStatus.Completed,
            OperationStatus.Failed => OrchestrationRuntimeStatus.Failed,
            OperationStatus.NotStarted => OrchestrationRuntimeStatus.Pending,
            OperationStatus.Running => OrchestrationRuntimeStatus.Running,
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
}
