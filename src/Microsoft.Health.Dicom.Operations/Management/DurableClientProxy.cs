// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Operations.Durable;
using Microsoft.Health.Dicom.Operations.Extensions;
using Microsoft.Health.Dicom.Operations.Indexing.Models;

namespace Microsoft.Health.Dicom.Operations.Management
{
    /// <summary>
    /// Represents a set of Azure Functions that servce as a proxy for the <see cref="IDurableOrchestrationClient"/>.
    /// </summary>
    public class DurableClientProxy
    {
        internal static readonly ImmutableHashSet<OperationType> PublicOperationTypes = ImmutableHashSet.Create(OperationType.Reindex);

        private readonly JsonSerializerOptions _jsonOptions;

        public DurableClientProxy(IOptions<JsonSerializerOptions> jsonOptions)
            => _jsonOptions = EnsureArg.IsNotNull(jsonOptions?.Value, nameof(jsonOptions));

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
        /// A task representing the <see cref="GetStatusAsync"/> operation. The value of its
        /// <see cref="Task{TResult}.Result"/> property contains the status of the DICOM operation
        /// with the specified <paramref name="instanceId"/>, if found; otherwise <see cref="BadRequestResult"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="request"/>, <paramref name="client"/>, or <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">The host is shutting down or the connection was aborted.</exception>
        [FunctionName(nameof(GetStatusAsync))]
        public async Task<HttpResponseMessage> GetStatusAsync(
            [HttpTrigger(AuthorizationLevel.Function, "GET", Route = "Orchestrations/Instances/{instanceId}")] HttpRequest request,
            [DurableClient] IDurableOrchestrationClient client,
            Guid instanceId,
            ILogger logger,
            CancellationToken hostCancellationToken = default)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(logger, nameof(logger));

            using CancellationTokenSource source = request.CreateRequestAbortedLinkedTokenSource(hostCancellationToken);

            logger.LogInformation("Querying orchestration instance with ID '{InstanceId}'", instanceId);

            // GetStatusAsync doesn't accept a token, so the best we can do is cancel before execution
            source.Token.ThrowIfCancellationRequested();

            InternalOperationStatus status = await GetOperationStatusAsync(client, instanceId);
            return status != null && PublicOperationTypes.Contains(status.Type)
                ? new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(status, _jsonOptions), Encoding.UTF8, MediaTypeNames.Application.Json),
                }
                : new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
        }

        private static async Task<InternalOperationStatus> GetOperationStatusAsync(IDurableOrchestrationClient client, Guid instanceId)
        {
            DurableOrchestrationStatus status = await client.GetStatusAsync(OperationId.ToString(instanceId), showInput: true);
            if (status == null)
            {
                return null;
            }

            OperationType type = status.GetOperationType();
            OperationRuntimeStatus runtimeStatus = status.GetOperationRuntimeStatus();
            OperationProgress progress = GetOperationProgress(type, status);
            return new InternalOperationStatus
            {
                CreatedTime = status.CreatedTime,
                LastUpdatedTime = status.LastUpdatedTime,
                OperationId = instanceId,
                PercentComplete = runtimeStatus == OperationRuntimeStatus.Completed ? 100 : progress.PercentComplete,
                ResourceIds = progress.ResourceIds ?? Array.Empty<string>(),
                Status = runtimeStatus,
                Type = type,
            };
        }

        private static OperationProgress GetOperationProgress(OperationType type, DurableOrchestrationStatus status)
        {
            switch (type)
            {
                case OperationType.Reindex:
                    ReindexInput reindexInput = status.Input?.ToObject<ReindexInput>() ?? new ReindexInput();
                    return reindexInput.GetProgress();
                default:
                    return new OperationProgress();
            }
        }
    }
}
