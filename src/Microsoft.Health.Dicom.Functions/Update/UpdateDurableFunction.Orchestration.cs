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
using Microsoft.Health.Dicom.Functions.Update.Models;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Update;

public partial class UpdateDurableFunction
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
    /// <returns>A task representing the <see cref="UpdateInstancesAsync"/> operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="FormatException">Orchestration instance ID is invalid.</exception>
    [FunctionName(nameof(UpdateInstancesAsync))]
    public async Task UpdateInstancesAsync(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context)).ThrowIfInvalidOperationId();
        logger = context.CreateReplaySafeLogger(EnsureArg.IsNotNull(logger, nameof(logger)));

        UpdateCheckpoint input = context.GetInput<UpdateCheckpoint>();

        // Backfill batching options
        input.Batching ??= new BatchingOptions
        {
            MaxParallelCount = _options.MaxParallelBatches,
            Size = _options.BatchSize,
        };

        if (input.StudyInstanceUids.Any())
        {
            string studyInstanceUid = input.StudyInstanceUids[0];

            IReadOnlyList<long> instanceWatermarks = await context.CallActivityWithRetryAsync<IReadOnlyList<long>>(
                nameof(GetInstanceWatermarksInStudyAsync),
                _options.RetryOptions,
                new GetInstanceArguments(input.PartitionKey, studyInstanceUid));
        }
        else
        {
            // Completed
        }



        //    // Fetch the set of query tags that require re-indexing
        //    IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags = await GetOperationQueryTagsAsync(context, input);
        //    logger.LogInformation(
        //        "Found {Count} extended query tag paths to re-index {{{TagPaths}}}.",
        //        queryTags.Count,
        //        string.Join(", ", queryTags.Select(x => x.Path)));

        //    List<int> queryTagKeys = queryTags.Select(x => x.Key).ToList();
        //    if (queryTags.Count > 0)
        //    {
        //        IReadOnlyList<WatermarkRange> batches = await context.CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
        //            nameof(GetInstanceBatchesV2Async),
        //            _options.RetryOptions,
        //            new BatchCreationArguments(input.Completed?.Start - 1, input.Batching.Size, input.Batching.MaxParallelCount));

        //        if (batches.Count > 0)
        //        {
        //            // Note that batches are in reverse order because we start from the highest watermark
        //            var batchRange = new WatermarkRange(batches[^1].Start, batches[0].End);

        //            logger.LogInformation("Beginning to re-index the range {Range}.", batchRange);
        //            await Task.WhenAll(batches
        //                .Select(x => context.CallActivityWithRetryAsync(
        //                    nameof(ReindexBatchV2Async),
        //                    _options.RetryOptions,
        //                    new ReindexBatchArguments(queryTags, x))));

        //            // Create a new orchestration with the same instance ID to process the remaining data
        //            logger.LogInformation("Completed re-indexing the range {Range}. Continuing with new execution...", batchRange);

        //            WatermarkRange completed = input.Completed.HasValue
        //                ? new WatermarkRange(batchRange.Start, input.Completed.Value.End)
        //                : batchRange;

        //            context.ContinueAsNew(
        //                new ReindexCheckpoint
        //                {
        //                    Batching = input.Batching,
        //                    Completed = completed,
        //                    CreatedTime = input.CreatedTime ?? await context.GetCreatedTimeAsync(_options.RetryOptions),
        //                    QueryTagKeys = queryTagKeys,
        //                });
        //        }
        //        else
        //        {
        //            IReadOnlyList<int> completed = await context.CallActivityWithRetryAsync<IReadOnlyList<int>>(
        //                nameof(CompleteUpdateInstanceAsync),
        //                _options.RetryOptions,
        //                queryTagKeys);

        //            logger.LogInformation(
        //                "Completed re-indexing for the following extended query tags {{{QueryTagKeys}}}.",
        //                string.Join(", ", completed));
        //        }
        //    }
        //    else
        //    {
        //        logger.LogWarning(
        //            "Could not find any query tags for the re-indexing operation '{OperationId}'.",
        //            context.InstanceId);
        //    }
        //}
    }
