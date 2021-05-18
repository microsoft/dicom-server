// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
using Microsoft.Health.Dicom.Functions.Indexing.Configuration;
using Microsoft.Health.Dicom.Functions.Indexing.Models;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    /// <summary>
    /// Represents the Azure Durable Functions that perform the re-indexing of previously added DICOM instances
    /// based on new tags configured by the user.
    /// </summary>
    public class ReindexFunction
    {
        private readonly ReindexConfiguration _reindexConfig;

        // TODO: Leverages services for fulfilling the various activies
        public ReindexFunction(IOptions<IndexingConfiguration> configOptions)
        {
            EnsureArg.IsNotNull(configOptions, nameof(configOptions));
            _reindexConfig = configOptions.Value.Add;
        }

        [FunctionName(nameof(RunOrchestrator))]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(log, nameof(log));
            long operationKey = long.Parse(context.InstanceId);

            log = context.CreateReplaySafeLogger(log);

            log.LogInformation("Beginning to re-index data");

            while (true)
            {
                IReadOnlyList<long> watermarks = await context.CallActivityAsync<IReadOnlyList<long>>(nameof(FetchWatarmarksAsync), context.InstanceId);
                if (watermarks.Count == 0)
                {
                    break;
                }
                // Fetch Query Tas
                IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags = await context.CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(FetchQueryTagsAsync),
                    context.InstanceId);

                if (queryTags.Count == 0)
                {
                    break;
                }

                // Reindex

                // TODO: process them parallel
                foreach (long watermark in watermarks)
                {
                    await context.CallActivityAsync(nameof(ReindexActivityAsync),
                     new ReindexActivityInput() { TagEntries = queryTags, Watarmark = watermark });
                }

                // TODO: Handle possibly different offsets for resumed tags
                await context.CallActivityAsync(nameof(UpdateProgressAsync),
                    new ReindexProgress { NextWatermark = watermarks.Min() - 1, OperationKey = operationKey });

            }

            log.LogInformation($"Completed re-indexing of data on operation {operationKey}");
        }

        [FunctionName(nameof(FetchQueryTagsAsync))]
        public Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> FetchQueryTagsAsync([ActivityTrigger] string instanceId, ILogger log)
        {
            EnsureArg.IsNotNullOrWhiteSpace(instanceId, nameof(instanceId));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Querying for tags assigned to {InstanceId}", instanceId);

            // Query SQL for query tags who have been tagged with the given Instance ID for adding
            return Task.FromResult<IReadOnlyList<ExtendedQueryTagStoreEntry>>(Array.Empty<ExtendedQueryTagStoreEntry>());
        }

        [FunctionName(nameof(FetchWatarmarksAsync))]
        public Task<IReadOnlyList<long>> FetchWatarmarksAsync([ActivityTrigger] string instanceId, ILogger log)
        {
            EnsureArg.IsNotNullOrWhiteSpace(instanceId, nameof(instanceId));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Querying for tags assigned to {InstanceId}", instanceId);

            // Query SQL for query tags who have been tagged with the given Instance ID for adding
            return Task.FromResult<IReadOnlyList<long>>(Array.Empty<long>());
        }

        [FunctionName(nameof(UpdateProgressAsync))]
        public Task UpdateProgressAsync([ActivityTrigger] ReindexProgress progress, ILogger log)
        {
            EnsureArg.IsNotNull(progress, nameof(progress));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Updating progress from re-index job for the following tags: {Tags}", string.Join(", ", progress.Keys));

            // Write to SQL the updated progress metrics
            // If the watermark has hit the end, change to "Added"
            return Task.CompletedTask;
        }

        [FunctionName(nameof(ReindexActivityAsync))]
        public static Task ReindexActivityAsync([ActivityTrigger] ReindexActivityInput input, ILogger log)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation($"Processing watermark {input.Watarmark}");

            // Read from Blob and write to SQL
            // Any exceptions should be appened to SQL too for diagnosis later
            return Task.CompletedTask;
        }




        [FunctionName(nameof(StartReindexOperation))]
        public async Task StartReindexOperation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = nameof(StartReindexOperation))] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log)
        {
            EnsureArg.IsNotNull(req, nameof(req));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(log, nameof(log));
            string operationId = req.Query["operationId"];
            log.LogInformation($"Start processing operation on {operationId}");
            await client.StartNewAsync(nameof(RunOrchestrator), instanceId: operationId);
        }

        private static Task<(string InstanceId, long End)> GetPendingJobParametersAsync()
        {
            // Fetch the Job ID of the pending reindex job
            return Task.FromResult((Guid.NewGuid().ToString(), 12345L));
        }
    }
}
