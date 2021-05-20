// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
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
    public class ReindexOperation
    {
        private readonly ReindexConfiguration _reindexConfig;
        private readonly ITagReindexOperationStore _tagOperationStore;
        private readonly IInstanceReindexer _instanceReindexer;

        public ReindexOperation(IOptions<IndexingConfiguration> configOptions,
            ITagReindexOperationStore tagOperationStore,
            IInstanceReindexer instanceReindexer)
        {
            EnsureArg.IsNotNull(configOptions, nameof(configOptions));
            EnsureArg.IsNotNull(tagOperationStore, nameof(tagOperationStore));
            EnsureArg.IsNotNull(instanceReindexer, nameof(instanceReindexer));
            _reindexConfig = configOptions.Value.Add;
            _tagOperationStore = tagOperationStore;
            _instanceReindexer = instanceReindexer;
        }

        [FunctionName(nameof(RunOrchestratorAsync))]
        public async Task RunOrchestratorAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            logger = context.CreateReplaySafeLogger(logger);
            string operationId = context.InstanceId;

            logger.LogInformation("Beginning to run orchestrator on {operationId}", operationId);

            // TODO: should start a new orchestraion instead of while loop
            while (true)
            {
                IReadOnlyList<long> watermarks = await context
                    .CallActivityAsync<IReadOnlyList<long>>(nameof(GetNextWatermarksOfOperationAsync), operationId);
                if (watermarks.Count == 0)
                {
                    break;
                }

                IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags = await context
                    .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(nameof(GetExtendedQueryTagsOfOperationAsync), operationId);

                if (queryTags.Count == 0)
                {
                    break;
                }

                // Reindex
                // TODO: process them parallel
                foreach (long watermark in watermarks)
                {
                    await context
                        .CallActivityAsync(nameof(ReindexInstanceAsync), new ReindexInstanceAsync() { TagEntries = queryTags, Watermark = watermark });
                }

                await context.CallActivityAsync(nameof(UpdateEndWatermarkOfOperationAsync),
                    new ReindexProgress { NextWatermark = watermarks.Min() - 1, OperationId = operationId });

            }

            await context.CallActivityAsync(nameof(CompleteOperationAsync), operationId);
            logger.LogInformation("Completed to run orchestrator on {operationId}", operationId);
        }

        [FunctionName(nameof(CompleteOperationAsync))]
        public Task CompleteOperationAsync([ActivityTrigger] string operationId, ILogger log)
        {
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Completing Reindex operation {operationId}", operationId);
            return _tagOperationStore.CompleteOperationAsync(operationId);
        }

        [FunctionName(nameof(GetExtendedQueryTagsOfOperationAsync))]
        public async Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetExtendedQueryTagsOfOperationAsync([ActivityTrigger] string operationId, ILogger log)
        {
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Getting extended query tags of {operationId}", operationId);
            var entries = await _tagOperationStore.GetEntriesOfOperationAsync(operationId);
            // only process tags which is on Processing
            return entries.Where(x => x.Status == TagOperationStatus.Processing).Select(y => y.TagStoreEntry).ToList();
        }

        [FunctionName(nameof(GetNextWatermarksOfOperationAsync))]
        public Task<IReadOnlyList<long>> GetNextWatermarksOfOperationAsync([ActivityTrigger] string operationId, ILogger logger)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            logger.LogInformation("Getting next watermarks of operation {operationId}", operationId);
            return _tagOperationStore.GetNextWatermarksOfOperationAsync(operationId, _reindexConfig.BatchSize * _reindexConfig.MaxParallelBatches);
        }

        [FunctionName(nameof(UpdateEndWatermarkOfOperationAsync))]
        public Task UpdateEndWatermarkOfOperationAsync([ActivityTrigger] ReindexProgress progress, ILogger log)
        {
            EnsureArg.IsNotNull(progress, nameof(progress));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation($"Updating end watermark of operation: {progress.OperationId}"); ;
            return _tagOperationStore.UpdateEndWatermarkOfOperationAsync(progress.OperationId, progress.NextWatermark);
        }

        /// <summary>
        /// Reindex  Dicom instance.
        /// </summary>
        /// <param name="input">The input</param>
        /// <param name="logger">The log.</param>
        /// <returns>The task</returns>
        [FunctionName(nameof(ReindexInstanceAsync))]
        public Task ReindexInstanceAsync([ActivityTrigger] ReindexInstanceAsync input, ILogger logger)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(logger, nameof(logger));

            logger.LogInformation("Reindexing with {input}", input);
            return _instanceReindexer.ReindexInstanceAsync(input.TagEntries, input.Watermark);
        }

        /// <summary>
        /// Start reinex operation.
        /// </summary>
        /// <param name="request">The http request.</param>
        /// <param name="client">The client.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(StartReindexOperationAsync))]
        public async Task StartReindexOperationAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = nameof(StartReindexOperationAsync))] HttpRequest request,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger logger)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(logger, nameof(logger));
            string operationId = request.Query["operationId"];
            logger.LogInformation($"Start processing reindex operation {operationId}");
            await client.StartNewAsync(nameof(RunOrchestratorAsync), instanceId: operationId);
        }

    }
}
