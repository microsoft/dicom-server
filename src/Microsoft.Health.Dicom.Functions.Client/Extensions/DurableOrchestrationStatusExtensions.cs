// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Functions.Client.Extensions
{
    internal static class DurableOrchestrationStatusExtensions
    {
        public static OperationType GetOperationType(this DurableOrchestrationStatus durableOrchestrationStatus)
        {
            EnsureArg.IsNotNull(durableOrchestrationStatus, nameof(durableOrchestrationStatus));

            return durableOrchestrationStatus.Name != null &&
                durableOrchestrationStatus.Name.StartsWith(FunctionNames.ReindexInstances, StringComparison.OrdinalIgnoreCase)
                ? OperationType.Reindex
                : OperationType.Unknown;
        }

        public static OperationRuntimeStatus GetOperationRuntimeStatus(this DurableOrchestrationStatus durableOrchestrationStatus)
        {
            EnsureArg.IsNotNull(durableOrchestrationStatus, nameof(durableOrchestrationStatus));

            return durableOrchestrationStatus.RuntimeStatus switch
            {
                OrchestrationRuntimeStatus.Pending => OperationRuntimeStatus.NotStarted,
                OrchestrationRuntimeStatus.Running => OperationRuntimeStatus.Running,
                OrchestrationRuntimeStatus.ContinuedAsNew => OperationRuntimeStatus.Running,
                OrchestrationRuntimeStatus.Completed => OperationRuntimeStatus.Completed,
                OrchestrationRuntimeStatus.Failed => OperationRuntimeStatus.Failed,
                OrchestrationRuntimeStatus.Canceled => OperationRuntimeStatus.Canceled,
                OrchestrationRuntimeStatus.Terminated => OperationRuntimeStatus.Canceled,
                _ => OperationRuntimeStatus.Unknown
            };
        }
    }
}
