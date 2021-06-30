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
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Functions.Indexing.Models;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    public partial class ReindexDurableFunction
    {
        /// <summary>
        ///  The orchestration function to reindex tags.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(ReindexTagsAsync))]
        public async Task ReindexTagsAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            logger = context.CreateReplaySafeLogger(logger);
            var input = context.GetInput<IReadOnlyCollection<int>>();
            await context.CallActivityAsync(nameof(UpdateSchemaVersionAsync), null);
            var tagEntries = await context.CallActivityAsync<IReadOnlyCollection<ExtendedQueryTagStoreEntry>>(nameof(GetTagStoreEntriesAsync), input);
            var watermarkRange = await context.CallActivityAsync<WatermarkRange>(nameof(GetReindexWatermarkRangeAsync), null);
            ReindexOperation reindexOperation = new ReindexOperation()
            {
                OperationId = context.InstanceId,
                StoreEntries = tagEntries,
                WatermarkRange = watermarkRange
            };

            await context.CallSubOrchestratorAsync(nameof(SubReindexTagsAsync), input: reindexOperation);
        }

        /// <summary>
        /// The orchestration function to Reindex Extended Query Tags.
        /// </summary>
        /// <param name="context">the context.</param>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        [FunctionName(nameof(SubReindexTagsAsync))]
        public async Task SubReindexTagsAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            logger = context.CreateReplaySafeLogger(logger);

            ReindexOperation reindexOperation = context.GetInput<ReindexOperation>();

            if (reindexOperation.WatermarkRange != null
                && reindexOperation.WatermarkRange.Start >= 0
                && reindexOperation.WatermarkRange.End >= 0)
            {

                long newEnd = await ReindexNextSegmentAsync(context, reindexOperation);

                if (reindexOperation.WatermarkRange.Start <= newEnd)
                {
                    ReindexOperation newInput = new ReindexOperation
                    {
                        WatermarkRange = new WatermarkRange(reindexOperation.WatermarkRange.Start, newEnd),
                        StoreEntries = reindexOperation.StoreEntries
                    };

                    context.StartNewOrchestration(nameof(SubReindexTagsAsync), input: newInput);
                }

            }
            await context.CallActivityAsync(nameof(CompleteReindexingTagsAsync), reindexOperation.StoreEntries.Select(x => x.Key).ToList());
        }

        private async Task<long> ReindexNextSegmentAsync(
            IDurableOrchestrationContext context,
            ReindexOperation reindexOperation)
        {
            // Note that StartWatermark and EndWatermark are Inclusive
            long start = reindexOperation.WatermarkRange.Start;
            long end = reindexOperation.WatermarkRange.End;

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
                        ReindexInstanceInput reindexInstanceInput = new ReindexInstanceInput
                        {
                            TagStoreEntries = reindexOperation.StoreEntries,
                            WatermarkRange = new WatermarkRange(batchStart, end)
                        };
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

            return end;
        }
    }
}
