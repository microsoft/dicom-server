// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Extensions;
using Microsoft.Health.Dicom.Functions.Indexing.Models;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    public partial class ReindexDurableFunction
    {
        /// <summary>
        /// Asynchronously starts the creation of an index for the provided query tags over the previously added data.
        /// </summary>
        /// <param name="request">An <see cref="HttpRequestMessage"/> containing the query tags.</param>
        /// <param name="client">A client for interacting with the Azure Durable Functions extension.</param>
        /// <param name="logger">A diagnostic logger.</param>
        /// <param name="hostCancellationToken">
        /// The token to monitor for cancellation requests from the Azure Functions host.
        /// The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the <see cref="StartReindexingInstancesAsync"/> operation.
        /// The value of its <see cref="Task{TResult}.Result"/> property contains the <see cref="HttpResponseMessage"/>
        /// whose body encodes the resulting orchestration instance ID.
        /// </returns>
        // TODO: Replace Anonymous with auth for all HTTP endpoints
        [FunctionName(nameof(StartReindexingInstancesAsync))]
        public async Task<HttpResponseMessage> StartReindexingInstancesAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "Post", Route = "extendedquerytags/reindex")] HttpRequest request,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger logger,
            CancellationToken hostCancellationToken = default)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(logger, nameof(logger));

            using CancellationTokenSource source = request.CreateRequestAbortedLinkedTokenSource(hostCancellationToken);

            // TODO: In .NET 5, use HttpRequestJsonExtensions.ReadFromJsonAsync which can gracefully handle different
            //       encodings. Here we'll expect a UTF-8 encoding which we can control as the client.
            IReadOnlyList<ExtendedQueryTagReference> tags;
            try
            {
                tags = await JsonSerializer.DeserializeAsync<IReadOnlyList<ExtendedQueryTagReference>>(request.Body, _jsonOptions, source.Token);
                if (tags == null || tags.Count == 0)
                {
                    throw new JsonException("Expected a JSON array with at least 1 element.");
                }
            }
            catch (JsonException e)
            {
                logger.LogError(e, "Cannot deserialize extended query tag keys.");
                return new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest };
            }

            logger.LogInformation("Received request to index {tagCount} extended query tag keys {{{extendedQueryTagKeys}}}.",
                tags.Count,
                string.Join(", ", tags));

            // Start the re-indexing orchestration
            Guid instanceGuid = _guidFactory.Create();
            string instanceId = await client.StartNewAsync(
                nameof(ReindexInstancesAsync),
                OperationId.ToString(instanceGuid),
                new ReindexInput { QueryTags = tags });

            logger.LogInformation("Successfully started new orchestration instance with ID '{InstanceId}'.", instanceId);

            // Associate the tags to the operation and confirm their processing
            IReadOnlyList<ExtendedQueryTagStoreEntry> confirmedTags = await _extendedQueryTagStore.AssignReindexingOperationAsync(
                tags.Select(x => x.Key).ToList(),
                instanceGuid,
                returnIfCompleted: true,
                cancellationToken: source.Token);

            return confirmedTags.Count == 0
                ? new HttpResponseMessage { StatusCode = HttpStatusCode.Conflict }
                : new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Accepted,
                    Content = new StringContent(instanceId, Encoding.UTF8, MediaTypeNames.Text.Plain)
                };
        }
    }
}
