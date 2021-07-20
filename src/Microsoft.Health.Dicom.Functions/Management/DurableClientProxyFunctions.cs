// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Functions.Extensions;
using Microsoft.Health.Dicom.Functions.Indexing;

namespace Microsoft.Health.Dicom.Functions.Management
{
    /// <summary>
    /// Represents a set of Azure Functions that servce as a proxy for the <see cref="IDurableOrchestrationClient"/>.
    /// </summary>
    public static class DurableClientProxyFunctions
    {
        internal static readonly ImmutableHashSet<string> PublicOperationTypes = ImmutableHashSet.Create(
            nameof(ReindexDurableFunction.ReindexInstancesAsync));

        /// <summary>
        /// Gets the status of an orchestration instance.
        /// </summary>
        /// <remarks>
        /// Only public-facing orchestrations can be returned by this operation.
        /// </remarks>
        /// <param name="request">The incoming HTTP request.</param>
        /// <param name="client">The client for interacting the the Durable Functions extension.</param>
        /// <param name="instanceId">The unique ID for the orchestration instance.</param>
        /// <param name="logger">An <see cref="ILogger"/> for logging information throughout execution.</param>
        /// <param name="hostCancellationToken">
        /// The token to monitor for cancellation requests from the Azure Functions host.
        /// The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the <see cref="GetStatusAsync(HttpRequest, IDurableOrchestrationClient, string, ILogger, CancellationToken)"/>
        /// operation. The value of its <see cref="Task{TResult}.Result"/> property contains the status of the orchestration
        /// instance with the specified <paramref name="instanceId"/>, if found; otherwise <see cref="BadRequestResult"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="request"/>, <paramref name="client"/>, or <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">The host is shutting down or the connection was aborted.</exception>
        // TODO: Replace Anonymous with auth for all HTTP endpoints
        [FunctionName(nameof(GetStatusAsync))]
        public static async Task<IActionResult> GetStatusAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "Orchestrations/Instances/{instanceId}")] HttpRequest request,
            [DurableClient] IDurableOrchestrationClient client,
            string instanceId,
            ILogger logger,
            CancellationToken hostCancellationToken = default)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(logger, nameof(logger));

            using CancellationTokenSource source = request.CreateRequestAbortedLinkedTokenSource(hostCancellationToken);

            logger.LogInformation("Querying orchestration instance with ID '{InstanceId}'", instanceId);

            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return new BadRequestResult();
            }

            // GetStatusAsync doesn't accept a token, so the best we can do is cancel before execution
            source.Token.ThrowIfCancellationRequested();

            DurableOrchestrationStatus status = await client.GetStatusAsync(instanceId, showInput: false);
            return status != null && PublicOperationTypes.Contains(status.Name)
                ? new OkObjectResult(status)
                : new NotFoundResult();
        }
    }
}
