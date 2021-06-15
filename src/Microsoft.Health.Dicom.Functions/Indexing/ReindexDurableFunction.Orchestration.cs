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
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Functions.Indexing.Models;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    public partial class ReindexDurableFunction
    {
        /// <summary>
        ///  The orchestration function to add extended query tags.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(AddAndReindexTagsAsync))]
        public async Task AddAndReindexTagsAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            logger = context.CreateReplaySafeLogger(logger);
            await context.CallActivityAsync(nameof(FetchSchemaVersionAsync), null);
            var input = context.GetInput<IReadOnlyList<AddExtendedQueryTagEntry>>();
            var storeEntires = await context.CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(AddTagsAsync),
                input);
            ReindexOperation reindexOperation = await context.CallActivityAsync<ReindexOperation>(
                nameof(PrepareReindexingTagsAsync),
                new PrepareReindexingTagsInput { OperationId = context.InstanceId, TagKeys = storeEntires.Select(x => x.Key).ToList() });
            await context.CallSubOrchestratorAsync(nameof(ReindexTagsAsync), input: reindexOperation);
        }

        /// <summary>
        /// The orchestration function to Reindex Extended Query Tags.
        /// </summary>
        /// <param name="context">the context.</param>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        [FunctionName(nameof(ReindexTagsAsync))]
        public async Task ReindexTagsAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            logger = context.CreateReplaySafeLogger(logger);

            ReindexOperation reindexOperation = context.GetInput<ReindexOperation>();

            // EndWatermark < 0 means there are no instances.
            if (reindexOperation.EndWatermark >= 0)
            {
                IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags = await context
                    .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(nameof(GetProcessingTagsAsync), reindexOperation.OperationId);

                if (queryTags.Count > 0)
                {
                    long newEnd = await ReindexNextSegmentAsync(context, reindexOperation, queryTags);

                    if (reindexOperation.StartWatermark <= newEnd)
                    {
                        ReindexOperation newInput = new ReindexOperation
                        {
                            StartWatermark = reindexOperation.StartWatermark,
                            EndWatermark = newEnd,
                            StoreEntries = reindexOperation.StoreEntries
                        };

                        context.StartNewOrchestration(nameof(ReindexTagsAsync), input: newInput);
                    }
                }
            }
            await context.CallActivityAsync(nameof(CompleteReindexingTagsAsync), reindexOperation.OperationId);
        }

        private async Task<long> ReindexNextSegmentAsync(
            IDurableOrchestrationContext context,
            ReindexOperation reindexOperation,
            IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags)
        {
            // Reindex. Note that StartWatermark and EndWatermark are Inclusive
            long start = reindexOperation.StartWatermark;
            long end = reindexOperation.EndWatermark;

            if (start <= end)
            {
                List<Task> batches = new List<Task>();

                // pickup next segment and send tasks to activity
                for (int parallel = 0; parallel < _reindexConfig.MaxParallelBatches; parallel++)
                {
                    long batchStart = end - _reindexConfig.BatchSize + 1;
                    batchStart = Math.Max(batchStart, start);
                    if (batchStart <= end)
                    {
                        ReindexInstanceInput reindexInstanceInput = new ReindexInstanceInput { TagStoreEntries = queryTags, WatermarkRange = (Start: batchStart, End: end) };
                        batches.Add(context.CallActivityAsync(nameof(ReindexInstancesAsync), reindexInstanceInput));
                        end = batchStart - 1;
                    }
                    else
                    {
                        break;
                    }
                }

                await Task.WhenAll(batches);
            }

            await context.CallActivityAsync(nameof(UpdateReindexProgressAsync),
                new UpdateReindexProgressInput { EndWatermark = end, OperationId = reindexOperation.OperationId });

            return end;
        }
    }
}
