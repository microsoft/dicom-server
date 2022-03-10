// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Dicom.Operations.Management
{
    /// <summary>
    /// Contains a collection of activities that serve as a proxy for the <see cref="IDurableOrchestrationClient"/>.
    /// </summary>
    public static class DurableOrchestrationClientActivity
    {
        /// <summary>
        /// Asynchronously retrieves the status of a given operation ID.
        /// </summary>
        /// <param name="context">The context for the activity.</param>
        /// <param name="client">A client for interacting with the durable task framework.</param>
        /// <param name="logger">A diagnostic logger.</param>
        /// <returns>
        /// A task representing the <see cref="GetInstanceStatusAsync"/> operation.
        /// The value of its <see cref="Task{TResult}.Result"/> property contains current status of the desired
        /// operation, if found; otherwise, <see langword="null"/>.
        /// </returns>
        [FunctionName(nameof(GetInstanceStatusAsync))]
        public static Task<DurableOrchestrationStatus> GetInstanceStatusAsync(
            [ActivityTrigger] IDurableActivityContext context,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(logger, nameof(logger));

            logger.LogInformation("Fetching status for operation ID '{OperationId}'.", context.InstanceId);

            GetInstanceStatusInput input = context.GetInput<GetInstanceStatusInput>();
            return client.GetStatusAsync(input.InstanceId, input.ShowHistory, input.ShowHistoryOutput, input.ShowInput);
        }
    }
}
