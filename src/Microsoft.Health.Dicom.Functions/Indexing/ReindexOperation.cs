// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Functions.Indexing.Configuration;
using Microsoft.Health.Dicom.Functions.Indexing.Models;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    /// <summary>
    /// Represents the Azure Durable Functions that perform the re-indexing of previously added DICOM instances
    /// based on new tags configured by the user.
    /// </summary>
    public partial class ReindexOperation
    {
        private readonly ReindexConfiguration _reindexConfig;
        private readonly ITagReindexOperationStore _tagOperationStore;
        private readonly IInstanceReindexer _instanceReindexer;
        private readonly IAddExtendedQueryTagService _addExtendedQueryTagService;

        public ReindexOperation(IOptions<IndexingConfiguration> configOptions,
            IAddExtendedQueryTagService addExtendedQueryTagService,
            ITagReindexOperationStore tagOperationStore,
            IInstanceReindexer instanceReindexer)
        {
            EnsureArg.IsNotNull(configOptions, nameof(configOptions));
            EnsureArg.IsNotNull(tagOperationStore, nameof(tagOperationStore));
            EnsureArg.IsNotNull(instanceReindexer, nameof(instanceReindexer));
            EnsureArg.IsNotNull(addExtendedQueryTagService, nameof(addExtendedQueryTagService));
            _reindexConfig = configOptions.Value.Add;
            _tagOperationStore = tagOperationStore;
            _instanceReindexer = instanceReindexer;
            _addExtendedQueryTagService = addExtendedQueryTagService;
        }

        /// <summary>
        ///  The orchestration function to add extended query tags.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(AddExtendedQueryTagsOrcAsync))]
        public async Task AddExtendedQueryTagsOrcAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            logger = context.CreateReplaySafeLogger(logger);
            var input = context.GetInput<IEnumerable<AddExtendedQueryTagEntry>>();
            await context.CallActivityAsync(nameof(AddExtendedQueryTagsAsync), new AddExtendedQueryTagsInput() { ExtendedQueryTagEntries = input, OperationId = context.InstanceId });
            await context.CallSubOrchestratorAsync(nameof(ReindexExtendedQueryTagsOrcAsync), context.InstanceId);
        }

        /// <summary>
        /// The orchestration function to Reindex Extended Query Tags.
        /// </summary>
        /// <param name="context">the context.</param>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        [FunctionName(nameof(ReindexExtendedQueryTagsOrcAsync))]
        public async Task ReindexExtendedQueryTagsOrcAsync(
           [OrchestrationTrigger] IDurableOrchestrationContext context,
           ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            logger = context.CreateReplaySafeLogger(logger);
            // TODO: should start a new orchestraion instead of while loop
            while (true)
            {
                IReadOnlyList<long> watermarks = await context
                    .CallActivityAsync<IReadOnlyList<long>>(nameof(GetNextWatermarksOfOperationAsync), context.InstanceId);
                if (watermarks.Count == 0)
                {
                    break;
                }

                IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags = await context
                    .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(nameof(GetExtendedQueryTagsOfOperationAsync), context.InstanceId);

                if (queryTags.Count == 0)
                {
                    break;
                }

                // Reindex
                // TODO: process them parallel
                foreach (long watermark in watermarks)
                {
                    await context
                        .CallActivityAsync(nameof(ReindexInstanceAsync), new ReindexInstanceInput() { TagEntries = queryTags, Watermarks = new long[] { watermark } });
                }

                await context.CallActivityAsync(nameof(UpdateEndWatermarkOfOperationAsync),
                    new UpdateEndWatermarkOfOperationInput { NextWatermark = watermarks.Min() - 1, OperationId = context.InstanceId });

            }

            await context.CallActivityAsync(nameof(CompleteOperationAsync), context.InstanceId);
            logger.LogInformation("Completed to run orchestrator on {operationId}", context.InstanceId);
        }

        /// <summary>
        /// The http trigger to add extended Query tags
        /// </summary>
        /// <param name="request">The http request.</param>
        /// <param name="client">The client.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(StartExtendedQueryTagAdditionAsync))]
        public async Task StartExtendedQueryTagAdditionAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "Post", Route = "extendedquerytags")] HttpRequestMessage request,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger logger)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(logger, nameof(logger));
            var extendedQueryTags = await request.Content.ReadAsAsync<IEnumerable<AddExtendedQueryTagEntry>>();
            logger.LogInformation($"Start adding extended query tags {string.Join(",", extendedQueryTags.Select(x => x.ToString()))}");
            await client.StartNewAsync(nameof(AddExtendedQueryTagsOrcAsync), instanceId: null, extendedQueryTags);

        }

    }
}
