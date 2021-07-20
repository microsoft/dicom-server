// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Functions.Indexing.Models;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    public partial class ReindexDurableFunction
    {
        /// <summary>
        /// Asynchronously starts the creation of an index for the provided query tags over the previously added data.
        /// </summary>
        /// <param name="request">An <see cref="HttpRequestMessage"/> containing the query tag keys.</param>
        /// <param name="client">A client for interacting with the Azure Durable Functions extension.</param>
        /// <param name="logger">A diagnostic logger.</param>
        /// <returns>
        /// A task representing the <see cref="StartReindexingInstancesAsync"/> operation.
        /// The value of its <see cref="Task{TResult}.Result"/> property contains the <see cref="HttpResponseMessage"/>
        /// whose body encodes the resulting orchestration instance ID.
        /// </returns>
        [FunctionName(nameof(StartReindexingInstancesAsync))]
        public async Task<HttpResponseMessage> StartReindexingInstancesAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "Post", Route = "extendedquerytags/reindex")] HttpRequestMessage request,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger logger)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(logger, nameof(logger));

            IReadOnlyList<int> extendedQueryTags = await request.Content.ReadAsAsync<IReadOnlyList<int>>();
            if (extendedQueryTags == null || extendedQueryTags.Count == 0)
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest };
            }

            logger.LogInformation("Received request to index {tagCount} extended query tag keys {{{extendedQueryTagKeys}}}.",
                extendedQueryTags.Count,
                string.Join(", ", extendedQueryTags));

            string instanceId = await client.StartNewAsync(nameof(ReindexInstancesAsync), new ReindexInput { QueryTagKeys = extendedQueryTags });

            logger.LogInformation("Successfully started new orchestration instance with ID '{InstanceId}'.", instanceId);
            return new HttpResponseMessage
            {
                Content = new StringContent(instanceId, Encoding.UTF8, MediaTypeNames.Text.Plain)
            };
        }
    }
}
