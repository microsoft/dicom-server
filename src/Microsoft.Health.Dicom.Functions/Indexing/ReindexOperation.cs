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
using Microsoft.Health.Dicom.Core.Features.Reindex;
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

        private readonly IReindexTagStore _reindexTagStore;
        private readonly IReindexService _reindexService;

        public ReindexOperation(IOptions<IndexingConfiguration> configOptions,
            IReindexTagStore reindexTagStore,
            IReindexService reindexService)
        {
            EnsureArg.IsNotNull(configOptions, nameof(configOptions));
            EnsureArg.IsNotNull(reindexTagStore, nameof(reindexTagStore));
            EnsureArg.IsNotNull(reindexService, nameof(reindexService));
            _reindexConfig = configOptions.Value.Add;
            _reindexTagStore = reindexTagStore;
            _reindexService = reindexService;
        }

        [FunctionName(nameof(RunOrchestrator))]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(log, nameof(log));
            string operationId = context.InstanceId;

            log = context.CreateReplaySafeLogger(log);

            log.LogInformation("Beginning to re-index data");

            // TODO: should start a new orchestraion instead of while loop
            while (true)
            {
                IReadOnlyList<long> watermarks = await context.CallActivityAsync<IReadOnlyList<long>>(nameof(FetchWatarmarksAsync), operationId);
                if (watermarks.Count == 0)
                {
                    break;
                }

                // TODO: what if failed here?
                // Fetch Query Tags
                IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags = await context.CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(FetchQueryTagsAsync),
                    operationId);

                if (queryTags.Count == 0)
                {
                    break;
                }

                // Reindex
                // TODO: process them parallel
                foreach (long watermark in watermarks)
                {
                    await context.CallActivityAsync(nameof(ReindexActivityAsync),
                     new ReindexActivityInput() { TagEntries = queryTags, Watermark = watermark });
                }

                await context.CallActivityAsync(nameof(UpdateProgressAsync),
                    new ReindexProgress { NextWatermark = watermarks.Min() - 1, OperationId = operationId });

            }

            await context.CallActivityAsync(nameof(CompleteReindexAsync), operationId);
            log.LogInformation($"Completed re-indexing of data on operation {operationId}");
        }

        [FunctionName(nameof(CompleteReindexAsync))]
        public async Task CompleteReindexAsync([ActivityTrigger] string operationId, ILogger log)
        {
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation($"Complete Reindex for tags assigned to {operationId}");
            await _reindexTagStore.CompleteReindexAsync(operationId);
        }

        [FunctionName(nameof(FetchQueryTagsAsync))]
        public async Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> FetchQueryTagsAsync([ActivityTrigger] long operationKey, ILogger log)
        {
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation($"Querying for tags assigned to {operationKey}");
            var result = await _reindexTagStore.GetTagsOnOperationAsync(operationKey);

            // only process Running tags on this operation
            return result.Where(x => x.Status == ReindexTagStatus.Running).Select(y => y.QueryTagStoreEntry).ToList();
        }

        [FunctionName(nameof(FetchWatarmarksAsync))]
        public async Task<IReadOnlyList<long>> FetchWatarmarksAsync([ActivityTrigger] long operationKey, ILogger log)
        {
            EnsureArg.IsNotNull(log, nameof(log));

            // TODO: make number of watermarks configurable
            var result = await _reindexTagStore.GetWatermarksAsync(operationKey, 1);
            return result;
        }

        [FunctionName(nameof(UpdateProgressAsync))]
        public Task UpdateProgressAsync([ActivityTrigger] ReindexProgress progress, ILogger log)
        {
            EnsureArg.IsNotNull(progress, nameof(progress));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation($"Updating progress from re-index job for operation: {progress.OperationId}"); ;
            _reindexTagStore.UpdateMaxWatermarkAsync(progress.OperationId, progress.NextWatermark);
            return Task.CompletedTask;
        }

        [FunctionName(nameof(ReindexActivityAsync))]
        public async Task ReindexActivityAsync([ActivityTrigger] ReindexActivityInput input, ILogger log)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation($"Processing watermark {input.Watermark}");
            await _reindexService.ReindexAsync(input.TagEntries, input.Watermark);
        }

        [FunctionName(nameof(StartReindexOperation))]
        public async Task StartReindexOperation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = nameof(StartReindexOperation))] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log)
        {
            EnsureArg.IsNotNull(req, nameof(req));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(log, nameof(log));
            string operationId = req.Query["operationId"];
            log.LogInformation($"Start processing operation {operationId}");
            await client.StartNewAsync(nameof(RunOrchestrator), instanceId: operationId);
        }

    }
}
