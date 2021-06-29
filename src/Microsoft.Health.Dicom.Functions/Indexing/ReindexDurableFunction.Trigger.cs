// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    public partial class ReindexDurableFunction
    {
        /// <summary>
        /// The http trigger to add extended Query tags
        /// </summary>
        /// <param name="request">The http request.</param>
        /// <param name="client">The client.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(StartReindexingTagsAsync))]
        public async Task<HttpResponseMessage> StartReindexingTagsAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "Post", Route = "extendedquerytags/reindex")] HttpRequestMessage request,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger logger)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(logger, nameof(logger));
            var extendedQueryTagKeys = await request.Content.ReadAsAsync<List<int>>();
            logger.LogInformation("Start reindexing extended query tags {extendedQueryTagKeys}", extendedQueryTagKeys);
            string instanceId = await client.StartNewAsync(nameof(ReindexTagsAsync), instanceId: null, extendedQueryTagKeys);
            logger.LogInformation("Started new orchestration with instanceId {instancId}", instanceId);

            return new HttpResponseMessage { Content = new StringContent(instanceId) };
        }
    }
}
