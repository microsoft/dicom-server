// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly ReindexConfiguration _clientConfig;

        // TODO: Leverages services for fulfilling the various activies
        public ReindexFunction(IOptions<IndexingConfiguration> configOptions)
        {
            EnsureArg.IsNotNull(configOptions, nameof(configOptions));
            _clientConfig = configOptions.Value.Add;
        }

        [FunctionName("ReindexV1")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(log, nameof(log));

            ReindexInput input = EnsureArg.IsNotNull(context.GetInput<ReindexInput>());
            log = context.CreateReplaySafeLogger(log);

            log.LogInformation("Beginning to re-index data between watermarks {Start} and {End}", input.Start, input.End);

            long current = input.Start;
            while (current < input.End)
            {
                // TODO: Adjust the "start" if all of the tags have indexed beyond "start"
                IReadOnlyList<ExtendedQueryTagEntry> queryTags = await context.CallActivityAsync<IReadOnlyList<ExtendedQueryTagEntry>>(
                    "ReindexV1_FetchQueryTags",
                    context.InstanceId);

                // TODO: Adjust batch size based on I/O in storage layers. Leverage an activity
                var batches = new List<Task>();
                for (int i = 0; i < input.MaxParallelBatches && current < input.End; i++)
                {
                    var batch = new ReindexBatch
                    {
                        Start = current,
                        End = Math.Min(current + input.BatchSize, input.End),
                        TagEntries = queryTags,
                    };

                    batches.Add(context.CallActivityAsync("ReindexV1_ProcessBatch", batch));

                    current = batch.End;
                }

                // Wait for all of the batches to complete
                await Task.WhenAll(batches);

                // TODO: Handle possibly different offsets for resumed tags
                IReadOnlyDictionary<string, ReindexProgress> progress = queryTags.ToDictionary(
                    x => x.Path,
                    _ => new ReindexProgress { NextWatermark = current });

                await context.CallActivityAsync("ReindexV1_UpdateProgress", progress);
            }

            log.LogInformation("Completed re-indexing of data between watermarks {Start} and {End}", input.Start, input.End);
        }

        [FunctionName("ReindexV1_FetchQueryTags")]
        public static Task<IReadOnlyList<ExtendedQueryTagEntry>> FetchQueryTagsAsync([ActivityTrigger] string instanceId, ILogger log)
        {
            EnsureArg.IsNotNullOrWhiteSpace(instanceId, nameof(instanceId));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Querying for tags assigned to {InstanceId}", instanceId);

            // Query SQL for query tags who have been tagged with the given Instance ID for adding
            return Task.FromResult<IReadOnlyList<ExtendedQueryTagEntry>>(Array.Empty<ExtendedQueryTagEntry>());
        }

        [FunctionName("ReindexV1_UpdateProgress")]
        public static Task UpdateProgressAsync([ActivityTrigger] IReadOnlyDictionary<string, ReindexProgress> progress, ILogger log)
        {
            EnsureArg.IsNotNull(progress, nameof(progress));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Updating progress from re-index job for the following tags: {Tags}", string.Join(", ", progress.Keys));

            // Write to SQL the updated progress metrics
            // If the watermark has hit the end, change to "Added"
            return Task.CompletedTask;
        }

        [FunctionName("ReindexV1_ProcessBatch")]
        public static Task ProcessBatchAsync([ActivityTrigger] ReindexBatch batch, ILogger log)
        {
            EnsureArg.IsNotNull(batch, nameof(batch));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Processing watermarks [{Start}, {End})", batch.Start, batch.End);

            // Read from Blob and write to SQL
            // Any exceptions should be appened to SQL too for diagnosis later
            return Task.CompletedTask;
        }

        [FunctionName("ReindexV1_Timer")]
        public async Task TimerStartAsync(
            [TimerTrigger("0 */5 * * * *")] TimerInfo timer,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log)
        {
            EnsureArg.IsNotNull(timer, nameof(timer));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(log, nameof(log));

            (string instanceId, long end) = await GetPendingJobParametersAsync();

            DurableOrchestrationStatus status = await client.GetStatusAsync(instanceId);
            if (status != null)
            {
                log.LogInformation("Job instance '{InstanceId}' was previously started and has status '{Status}'",
                    instanceId,
                    status.RuntimeStatus);
            }
            else
            {
                var input = new ReindexInput
                {
                    Start = 0,
                    End = end,
                    BatchSize = _clientConfig.BatchSize,
                    MaxParallelBatches = _clientConfig.MaxParallelBatches,
                };

                // Note: If the instance is already started, this will silently replace it
                await client.StartNewAsync("ReindexV1", instanceId, input);

                log.LogInformation("Started job instance '{InstanceId}'.", instanceId);
            }
        }

        private static Task<(string InstanceId, long End)> GetPendingJobParametersAsync()
        {
            // Fetch the Job ID of the pending reindex job
            return Task.FromResult((Guid.NewGuid().ToString(), 12345L));
        }
    }
}
