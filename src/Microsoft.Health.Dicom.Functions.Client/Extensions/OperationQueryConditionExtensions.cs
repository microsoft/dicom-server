// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Client.Extensions;

internal static class OperationQueryConditionExtensions
{
    public static OrchestrationStatusQueryCondition ForDurableFunctions<T>(this OperationQueryCondition<T> query, string continuationToken = null)
    {
        // TODO #73705: Modify page size when we add /operations endpoint
        EnsureArg.IsNotNull(query, nameof(query));

        // Aggressively resolve to validate input
        var statuses = query
            .Statuses
            .SelectMany(s => s.ToOrchestrationRuntimeStatuses())
            .Select(s => s != OrchestrationRuntimeStatus.Unknown ? s : throw new ArgumentOutOfRangeException(nameof(query)))
            .ToList();

        return new OrchestrationStatusQueryCondition
        {
            ContinuationToken = continuationToken,
            CreatedTimeFrom = query.CreatedTimeFrom,
            CreatedTimeTo = query.CreatedTimeTo,
            RuntimeStatus = statuses,
            ShowInput = true,
        };
    }
}
