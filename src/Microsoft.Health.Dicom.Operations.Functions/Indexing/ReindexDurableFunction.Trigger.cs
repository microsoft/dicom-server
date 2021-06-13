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
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Operations.Functions.Indexing
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
        [FunctionName(nameof(StartAddingTagsAsync))]
        public async Task<HttpResponseMessage> StartAddingTagsAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "Post", Route = "extendedquerytags")] HttpRequestMessage request,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger logger)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(logger, nameof(logger));
            var extendedQueryTags = await request.Content.ReadAsAsync<List<AddExtendedQueryTagEntry>>();
            logger.LogInformation("Start adding extended query tags {input}", extendedQueryTags);
            string instanceId = await client.StartNewAsync(nameof(AddAndReindexTagsAsync), instanceId: null, extendedQueryTags);
            logger.LogInformation("Started new orchestration with instanceId {instancId}", instanceId);

            // TODO: these code need to be updated based on contract to client.
            return new HttpResponseMessage { Content = new StringContent(instanceId) };
        }
    }
}
