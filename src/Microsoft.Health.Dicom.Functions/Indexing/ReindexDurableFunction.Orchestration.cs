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
    /// <summary>
    /// Represents the Azure Durable Functions that perform the re-indexing of previously added DICOM instances
    /// based on new tags configured by the user.
    /// </summary>
    public partial class ReindexDurableFunction
    {
        /// <summary>
        ///  The orchestration function to add extended query tags.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(AddExtendedQueryTagsOrchestrationAsync))]
        public async Task AddExtendedQueryTagsOrchestrationAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            logger = context.CreateReplaySafeLogger(logger);
            var input = context.GetInput<IEnumerable<AddExtendedQueryTagEntry>>();
            var storeEntires = await context.CallActivityAsync<IReadOnlyCollection<ExtendedQueryTagStoreEntry>>(
                nameof(AddExtendedQueryTagsActivityAsync),
                 input);
            ReindexOperation reindexOperation = await context.CallActivityAsync<ReindexOperation>(
                 nameof(StartReindexActivityAsync),
                 new StartReindexActivityInput() { OperationId = context.InstanceId, TagKeys = storeEntires.Select(x => x.Key) });
            await context.CallSubOrchestratorAsync(nameof(ReindexExtendedQueryTagsOrchestrationAsync), input: reindexOperation);
        }

        /// <summary>
        /// The orchestration function to Reindex Extended Query Tags.
        /// </summary>
        /// <param name="context">the context.</param>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        [FunctionName(nameof(ReindexExtendedQueryTagsOrchestrationAsync))]
        public async Task ReindexExtendedQueryTagsOrchestrationAsync([OrchestrationTrigger] IDurableOrchestrationContext context,
           ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            logger = context.CreateReplaySafeLogger(logger);

            IEnumerable<ExtendedQueryTagStoreEntry> queryTags = await context
                .CallActivityAsync<IEnumerable<ExtendedQueryTagStoreEntry>>(nameof(GetProcessingQueryTagsActivityAsync), context.InstanceId);

            if (queryTags.Any())
            {
                return;
            }

            ReindexOperation reindexOperation = context.GetInput<ReindexOperation>();
            long newEnd = await ReindexNextSectionAsync(context, reindexOperation, queryTags);

            if (reindexOperation.StartWatermark <= newEnd)
            {
                ReindexOperation newInput = new ReindexOperation()
                {
                    StartWatermark = reindexOperation.StartWatermark,
                    EndWatermark = newEnd,
                    QueryTags = reindexOperation.QueryTags
                };

                context.StartNewOrchestration(nameof(ReindexExtendedQueryTagsOrchestrationAsync), input: newInput);
            }
            else
            {
                await context.CallActivityAsync(nameof(CompleteReindexActivityAsync), reindexOperation.OperationId);
            }

            logger.LogInformation("Completed to run orchestrator on {operationId}", reindexOperation.OperationId);
        }

        private async Task<long> ReindexNextSectionAsync(IDurableOrchestrationContext context,
            ReindexOperation reindexOperation,
            IEnumerable<ExtendedQueryTagStoreEntry> queryTags)
        {

            // Reindex
            long start = reindexOperation.StartWatermark;
            long end = reindexOperation.EndWatermark;

            List<Task> batches = new List<Task>();

            // pickup next section and send task to activity
            for (int parallel = 0; parallel < _reindexConfig.MaxParallelBatches; parallel++)
            {

                long batchStart = end - _reindexConfig.BatchSize;
                batchStart = Math.Max(batchStart, start);
                if (batchStart <= end)
                {
                    ReindexInstanceActivityInput reindexInstanceInput = new ReindexInstanceActivityInput() { TagStoreEntries = queryTags, StartWatermark = batchStart, EndWatermark = end };
                    batches.Add(context.CallActivityAsync(nameof(ReindexInstanceActivityAsync), reindexInstanceInput));
                    end = batchStart - 1;
                }
                else
                {
                    break;
                }
            }

            await Task.WhenAll(batches);

            await context.CallActivityAsync(nameof(UpdateReindexProgressActivityAsync),
                new UpdateReindexProgressActivityInput { EndWatermark = end, OperationId = reindexOperation.OperationId });

            return end;
        }
    }
}
