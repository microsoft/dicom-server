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
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.Indexing.Models;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    public partial class ReindexDurableFunction
    {
        /// <summary>
        /// Asynchronously creates an index for the provided query tags over the previously added data.
        /// </summary>
        /// <remarks>
        /// Durable functions are reliable, and their implementations will be executed repeatedly over the lifetime of
        /// a single instance.
        /// </remarks>
        /// <param name="context">The context for the orchestration instance.</param>
        /// <param name="logger">A diagnostic logger.</param>
        /// <returns>A task representing the <see cref="ReindexInstancesAsync"/> operation.</returns>
        [FunctionName(nameof(ReindexInstancesAsync))]
        public async Task ReindexInstancesAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            logger = context.CreateReplaySafeLogger(logger);
            ReindexInput input = context.GetInput<ReindexInput>();

            // Determine the set of query tags that should be indexed and only continue if there is at least 1
            IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags = await context
                .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(nameof(GetQueryTagsAsync), input.QueryTagKeys);

            logger.LogInformation(
                "Found {Count} extended query tag paths to re-index {{{TagPaths}}}.",
                queryTags.Count,
                string.Join(", ", queryTags.Select(x => x.Path)));

            List<int> queryTagKeys = queryTags.Select(x => x.Key).ToList();
            if (queryTags.Count > 0)
            {
                List<WatermarkRange> batches = await GetBatchesAsync(context, input.Completed, logger);
                if (batches.Count > 0)
                {
                    // Note that batches are in reverse order because we start from the highest watermark
                    var batchRange = WatermarkRange.Between(batches[^1].Start, batches[0].End);

                    logger.LogInformation("Beginning to re-index the range {Range}.", batchRange);
                    await Task.WhenAll(batches
                        .Select(x => context.CallActivityAsync(
                            nameof(ReindexBatchAsync),
                            new ReindexBatch { QueryTags = queryTags, WatermarkRange = x })));

                    // Create a new orchestration with the same instance ID to process the remaining data
                    logger.LogInformation("Completed re-indexing the range {Range}. Continuing with new execution...", batchRange);

                    context.ContinueAsNew(
                        new ReindexInput
                        {
                            QueryTagKeys = queryTagKeys,
                            Completed = input.Completed.Count == 0 ? batchRange : WatermarkRange.Between(batchRange.Start, input.Completed.End),
                        });
                }
                else
                {
                    IReadOnlyList<int> completed = await context.CallActivityAsync<IReadOnlyList<int>>(
                        nameof(CompleteReindexingAsync),
                        queryTagKeys);

                    logger.LogInformation(
                        "Completed re-indexing for the following extended query tags {{{QueryTagKeys}}}.",
                        string.Join(", ", completed));
                }
            }
            else
            {
                logger.LogWarning(
                    "Could not find any query tags for the re-indexing operation '{OperationId}'.",
                    context.InstanceId);
            }
        }

        private async Task<List<WatermarkRange>> GetBatchesAsync(
            IDurableOrchestrationContext context,
            WatermarkRange completed,
            ILogger logger)
        {
            // If we haven't completed any range yet, fetch the maximum watermark from the database.
            // Otherwise, create a WatermarkRange based on the latest progress.
            long end;
            if (completed.Count > 0)
            {
                end = completed.Start;
                logger.LogInformation("Previous execution finished range {Range}.", completed);
            }
            else
            {
#pragma warning disable DF0108
                // TODO: Durable Function analyzer incorrectly detects DF0108. Remove when it's resolved
                end = (await context.CallActivityAsync<long>(nameof(GetMaxInstanceWatermarkAsync), input: null)) + 1;
                logger.LogInformation("Found maximum watermark {Max}.", end - 1);
#pragma warning restore DF0108
            }

            // Note that the watermark sequence starts at 1!
            var batches = new List<WatermarkRange>();
            for (; end > 1 && batches.Count < _options.MaxParallelBatches; end -= _options.BatchSize)
            {
                int count = (int)Math.Min(end - 1, _options.BatchSize);
                batches.Add(new WatermarkRange(end - count, count));
            }

            return batches;
        }
    }
}
