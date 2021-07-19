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
            IReadOnlyCollection<ExtendedQueryTagStoreEntry> queryTags = await context
                .CallActivityAsync<IReadOnlyCollection<ExtendedQueryTagStoreEntry>>(nameof(GetQueryTagsAsync), input.QueryTagKeys);

            List<int> queryTagKeys = queryTags.Select(x => x.Key).ToList();
            if (queryTags.Count > 0)
            {
                WatermarkRange? optionalRange = await GetNextRangeAsync(context, input.Progress);
                if (optionalRange.HasValue)
                {
                    WatermarkRange range = optionalRange.GetValueOrDefault();

                    logger.LogInformation("Beginning to re-index the range {Range}.", range);
                    await ReindexRangeAsync(context, range, queryTags);

                    // Create a new orchestration with the same instance ID to process the remaining data
                    logger.LogInformation("Completed re-indexing the range {Range}. Continuing with new execution...", range);

                    context.ContinueAsNew(
                        new ReindexInput
                        {
                            QueryTagKeys = queryTagKeys,
                            Progress = new ReindexProgress
                            {
                                CurrentWatermark = range.Start - 1,
                                MaxWatermark = input.Progress?.MaxWatermark ?? range.End,
                            },
                        });
                }
                else
                {
                    IReadOnlyCollection<int> completed = await context.CallActivityAsync<IReadOnlyCollection<int>>(
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

        private async Task<WatermarkRange?> GetNextRangeAsync(IDurableOrchestrationContext context, ReindexProgress progress)
        {
            // If we haven't made progress yet, fetch the maximum watermark from the database.
            // Otherwise, create a WatermarkRange based on the latest progress.
            // TODO: Durable Function analyzer incorrectly detects DF0108. Remove when it's resolved
#pragma warning disable DF0108
            long max = progress != null
                ? progress.CurrentWatermark
                : (await context.CallActivityAsync<long?>(nameof(GetMaxInstanceWatermarkAsync), input: null)).GetValueOrDefault();
#pragma warning restore DF0108

            if (max > 0)
            {
                int count = (int)Math.Min(max - 1, _options.MaxParallelCount);
                return new WatermarkRange(max - count, count);
            }
            else
            {
                return null;
            }
        }

        private Task ReindexRangeAsync(
            IDurableOrchestrationContext context,
            WatermarkRange range,
            IReadOnlyCollection<ExtendedQueryTagStoreEntry> queryTags)
        {
            (long start, long end) = range;

            var tasks = new List<Task>();
            for (; end > start && tasks.Count < _options.MaxParallelBatches; end -= _options.BatchSize)
            {
                int count = (int)Math.Min(end - start, _options.BatchSize);
                tasks.Add(context.CallActivityAsync(
                    nameof(ReindexBatchAsync),
                    new ReindexBatch
                    {
                        QueryTags = queryTags,
                        WatermarkRange = new WatermarkRange(end - count, count),
                    }));
            }

            return Task.WhenAll(tasks);
        }
    }
}
